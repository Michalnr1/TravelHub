using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

// Teraz dziedziczy z serwisu generycznego
public class ExpenseService : GenericService<Expense>, IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ITripService _tripService;
    private readonly IExchangeRateService _exchangeRateService;

    public ExpenseService(IExpenseRepository expenseRepository, IExchangeRateService exchangeRateService, ITripService tripService)
        : base(expenseRepository) // Przekazujemy repozytorium do serwisu generycznego
    {
        _expenseRepository = expenseRepository;
        _exchangeRateService = exchangeRateService;
        _tripService = tripService;
    }

    public new async Task<Expense> GetByIdAsync(object id)
    {
        if (id is int expenseId)
        {
            var result = await _expenseRepository.GetByIdWithParticipantsAsync(expenseId);

            if (result == null)
                throw new InvalidOperationException($"Entity of type Expense with id {id} was not found.");

            return result;
        }

        return await base.GetByIdAsync(id);
    }

    public new async Task<Expense> AddAsync(Expense entity)
    {
        return await AddAsync(entity, new List<ParticipantShareDto>());
    }

    public new async Task UpdateAsync(Expense entity)
    {
        await UpdateAsync(entity, new List<ParticipantShareDto>());
    }

    public async Task<Expense> AddAsync(Expense entity, IEnumerable<ParticipantShareDto> participantShares)
    {
        var shares = participantShares.ToList();

        if (shares.Any())
        {
            // Wywołujemy nową metodę pomocniczą do obliczenia i utworzenia linków
            entity.Participants = CalculateAndCreateParticipants(
                entity.Value,
                entity.Id,
                shares);
        }
        else
        {
            entity.Participants = new List<ExpenseParticipant>();
        }

        return await _expenseRepository.AddAsync(entity);
    }

    public async Task UpdateAsync(Expense existingExpense, IEnumerable<ParticipantShareDto> newParticipantShares)
    {
        // 1. Zidentyfikuj istniejące i nowe identyfikatory
        var existingParticipantLinks = existingExpense.Participants?.ToList() ?? new List<ExpenseParticipant>();
        var newSharesList = newParticipantShares.ToList();

        var currentParticipantIds = existingParticipantLinks.Select(ep => ep.PersonId).ToHashSet();
        var newParticipantIds = newSharesList.Select(s => s.PersonId).ToHashSet();

        // 2. Uczestnicy do usunięcia
        var participantsToRemove = existingParticipantLinks
            .Where(ep => !newParticipantIds.Contains(ep.PersonId))
            .ToList();

        // 3. Wykonaj faktyczne usunięcia
        foreach (var link in participantsToRemove)
        {
            existingExpense.Participants?.Remove(link);
        }

        // 4. Oblicz udziały dla wszystkich uczestników na nowo
        var calculatedParticipants = CalculateAndCreateParticipants(
            existingExpense.Value,
            existingExpense.Id,
            newSharesList);

        // 5. Dodaj/Aktualizuj pozostałe linki
        foreach (var newLink in calculatedParticipants)
        {
            var existingLink = existingExpense.Participants?.FirstOrDefault(ep => ep.PersonId == newLink.PersonId);

            if (existingLink != null)
            {
                // Aktualizuj istniejący link
                existingLink.Share = newLink.Share;
                existingLink.ActualShareValue = newLink.ActualShareValue;
            }
            else
            {
                // Dodaj nowy link
                existingExpense.Participants?.Add(newLink);
            }
        }

        // 6. Wywołaj generyczną aktualizację
        await _expenseRepository.UpdateAsync(existingExpense);
    }

    // Metody specyficzne dla Expense
    public async Task<IReadOnlyList<Expense>> GetUserExpensesAsync(string userId)
    {
        return await _expenseRepository.GetExpensesByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId)
    {
        return await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);
    }

    public async Task<TripExpensesSummaryDto> CalculateTripExpensesInTripCurrencyAsync(int tripId, CurrencyCode tripCurrency)
    {
        //var expenses = await _expenseRepository.GetByTripIdWithExchangeRatesAsync(tripId);
        var expenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);
        var tripExchangeRates = await _exchangeRateService.GetTripExchangeRatesAsync(tripId);

        var tripBaseRate = tripExchangeRates.FirstOrDefault(er => er.CurrencyCodeKey == tripCurrency);

        var summary = new TripExpensesSummaryDto
        {
            TripCurrency = tripCurrency
        };

        foreach (var expense in expenses)
        {
            var expenseCurrency = expense.ExchangeRate?.CurrencyCodeKey ?? tripCurrency;

            if (expenseCurrency == tripCurrency)
            {
                // Ta sama waluta - bez konwersji
                summary.TotalExpensesInTripCurrency += expense.Value;
                summary.ExpenseCalculations.Add(new ExpenseCalculationDto
                {
                    ExpenseId = expense.Id,
                    OriginalValue = expense.Value,
                    OriginalCurrency = expenseCurrency,
                    TargetCurrency = tripCurrency,
                    ExchangeRate = 1m,
                    ConvertedValue = expense.Value
                });
                continue;
            }

            // Konwersja waluty
            var expenseRate = expense.ExchangeRate;
            if (expenseRate == null || tripBaseRate == null)
            {
                // Brak kursów - traktujemy jako 1:1
                summary.TotalExpensesInTripCurrency += expense.Value;
                summary.ExpenseCalculations.Add(new ExpenseCalculationDto
                {
                    ExpenseId = expense.Id,
                    OriginalValue = expense.Value,
                    OriginalCurrency = expenseCurrency,
                    TargetCurrency = tripCurrency,
                    ExchangeRate = 1m,
                    ConvertedValue = expense.Value
                });
                continue;
            }

            // Oblicz przelicznik: waluta_podróży = waluta_wydatku * (kurs_wydatku / kurs_podróży)
            var conversionRate = expenseRate.ExchangeRateValue / tripBaseRate.ExchangeRateValue;
            var convertedValue = expense.Value * conversionRate;

            summary.TotalExpensesInTripCurrency += convertedValue;
            summary.ExpenseCalculations.Add(new ExpenseCalculationDto
            {
                ExpenseId = expense.Id,
                OriginalValue = expense.Value,
                OriginalCurrency = expenseCurrency,
                TargetCurrency = tripCurrency,
                ExchangeRate = conversionRate,
                ConvertedValue = convertedValue
            });
        }

        return summary;
    }

    public async Task<decimal> GetTotalExpensesInCurrencyAsync(int tripId, CurrencyCode targetCurrency)
    {
        var summary = await CalculateTripExpensesInTripCurrencyAsync(tripId, targetCurrency);
        return summary.TotalExpensesInTripCurrency;
    }

    public async Task<Expense?> GetExpenseByAccommodationIdAsync(int accommodationId)
    {
        return await _expenseRepository.GetExpenseByAccommodationIdAsync(accommodationId);
    }

    public async Task<BalanceDto> CalculateBalancesAsync(int tripId)
    {
        var expenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);
        var trip = await _tripService.GetByIdAsync(tripId);
        var participants = await _tripService.GetAllTripParticipantsAsync(tripId);

        if (trip == null)
            throw new InvalidOperationException($"Trip with id {tripId} not found");

        var tripCurrency = trip.CurrencyCode;

        var owesToOthers = new Dictionary<string, decimal>();
        var owedByOthers = new Dictionary<string, decimal>();
        var debtDetails = new List<DebtDetailDto>();

        // Setup dictionaries
        foreach (var participant in participants)
        {
            owesToOthers[participant.Id] = 0;
            owedByOthers[participant.Id] = 0;
        }

        foreach (var expense in expenses)
        {
            var paidBy = expense.PaidById;
            var expenseValueInTripCurrency = await ConvertToTripCurrencyAsync(expense.Value, expense.ExchangeRate, tripCurrency);

            if (expense.TransferredToId == null)
            {
                foreach (var participant in expense.Participants.Where(p => p.PersonId != paidBy))
                {
                    var shareInTripCurrency = await ConvertToTripCurrencyAsync(participant.ActualShareValue, expense.ExchangeRate, tripCurrency);

                    if (owesToOthers.ContainsKey(participant.PersonId))
                        owesToOthers[participant.PersonId] += shareInTripCurrency;

                    if (owedByOthers.ContainsKey(paidBy))
                        owedByOthers[paidBy] += shareInTripCurrency;

                    debtDetails.Add(new DebtDetailDto
                    {
                        FromPersonId = participant.PersonId,
                        FromPersonName = $"{participant.Person?.FirstName} {participant.Person?.LastName}",
                        ToPersonId = paidBy,
                        ToPersonName = $"{expense.PaidBy?.FirstName} {expense.PaidBy?.LastName}",
                        Amount = shareInTripCurrency
                    });
                }
            }
            else
            {
                var transferredTo = expense.TransferredToId;
                var transferAmountInTripCurrency = await ConvertToTripCurrencyAsync(expense.Value, expense.ExchangeRate, tripCurrency);

                if (owesToOthers.ContainsKey(paidBy))
                    owesToOthers[paidBy] = Math.Max(0, owesToOthers[paidBy] - transferAmountInTripCurrency);

                if (owedByOthers.ContainsKey(transferredTo))
                    owedByOthers[transferredTo] = Math.Max(0, owedByOthers[transferredTo] - transferAmountInTripCurrency);

                var existingDebt = debtDetails.FirstOrDefault(d => d.FromPersonId == paidBy && d.ToPersonId == transferredTo);

                if (existingDebt != null)
                {
                    existingDebt.Amount = Math.Max(0, existingDebt.Amount - transferAmountInTripCurrency);
                }
            }
        }

        var aggregatedDebts = debtDetails
            .GroupBy(d => new { d.FromPersonId, d.ToPersonId })
            .Select(g => new DebtDetailDto
            {
                FromPersonId = g.Key.FromPersonId,
                FromPersonName = g.First().FromPersonName,
                ToPersonId = g.Key.ToPersonId,
                ToPersonName = g.First().ToPersonName,
                Amount = g.Sum(x => x.Amount)
            })
            .Where(d => d.Amount > 0.01m)
            .ToList();

        var optimizedDebts = OptimizeDebts(aggregatedDebts);

        var participantBalances = participants.Select(p => new ParticipantBalanceDto
        {
            PersonId = p.Id,
            FullName = $"{p.FirstName} {p.LastName}",
            OwesToOthers = owesToOthers[p.Id],
            OwedByOthers = owedByOthers[p.Id],
            NetBalance = owedByOthers[p.Id] - owesToOthers[p.Id]
        }).ToList();

        return new BalanceDto
        {
            TripId = tripId,
            TripName = trip.Name,
            TripCurrency = tripCurrency,
            ParticipantBalances = participantBalances,
            DebtDetails = optimizedDebts
        };
    }

    private List<ExpenseParticipant> CalculateAndCreateParticipants(decimal expenseValue, int expenseId, IEnumerable<ParticipantShareDto> participantShares)
    {
        var sharesList = participantShares.ToList();
        var totalParticipantsCount = sharesList.Count;

        // Jeśli wartość wydatku wynosi 0, lub nie ma uczestników, zwracamy zerowe udziały
        if (expenseValue <= 0 || totalParticipantsCount == 0)
        {
            return sharesList.Select(s => new ExpenseParticipant
            {
                ExpenseId = expenseId,
                PersonId = s.PersonId,
                Share = 0.000m,
                ActualShareValue = 0.00m
            }).ToList();
        }

        // Wstępna walidacja inputów
        if (!ValidateInputShares(sharesList, expenseValue))
        {
            throw new InvalidOperationException("Suma udziałów przekracza dostępną wartość wydatku");
        }

        // Obliczanie domyślnego równego podziału
        var defaultSharePercentage = 1m / totalParticipantsCount;
        var equalShareAmount = expenseValue / totalParticipantsCount;

        var newParticipants = new List<ExpenseParticipant>();

        foreach (var shareDto in sharesList)
        {
            decimal finalShare = 0.000m;
            decimal finalValue = 0.00m;

            switch (shareDto.ShareType)
            {
                case 1: // Użytkownik wpisał KWOTĘ (ActualShareValue)
                    finalValue = Math.Round(shareDto.InputValue, 2);
                    // Oblicz procent: Kwota / Całkowita wartość wydatku (jeśli Value > 0)
                    finalShare = expenseValue > 0 ? Math.Round(finalValue / expenseValue, 3) : 0.000m;
                    break;

                case 2: // Użytkownik wpisał PROCENT (Share)
                    finalShare = Math.Round(shareDto.InputValue, 3);
                    // Oblicz kwotę: Procent * Całkowita wartość wydatku
                    finalValue = Math.Round(finalShare * expenseValue, 2);
                    break;

                case 0: // Równy podział (domyślny)
                default:
                    finalShare = Math.Round(defaultSharePercentage, 3);
                    finalValue = Math.Round(equalShareAmount, 2);
                    break;
            }

            newParticipants.Add(new ExpenseParticipant
            {
                ExpenseId = expenseId,
                PersonId = shareDto.PersonId,
                Share = finalShare,
                ActualShareValue = finalValue
            });
        }

        return newParticipants;
    }

    private bool ValidateInputShares(List<ParticipantShareDto> shares, decimal expenseValue)
    {
        if (expenseValue == 0.00m)
        {
            return shares.All(share => share.InputValue == 0.00m);
        }

        decimal totalShareValueInAmount = 0.00m;

        foreach (var share in shares)
        {
            switch (share.ShareType)
            {
                case 1: // Kwota
                    totalShareValueInAmount += share.InputValue;
                    break;
                case 2: // Procent
                    totalShareValueInAmount += share.InputValue * expenseValue;
                    break;
            }
        }

        const decimal tolerance = 0.01m;

        return Math.Abs(totalShareValueInAmount - expenseValue) <= tolerance;
    }

    private static Task<decimal> ConvertToTripCurrencyAsync(decimal amount, ExchangeRate? expenseRate, CurrencyCode tripCurrency)
    {
        // Jeśli brak kursu wydatku lub wydatek jest już w walucie podróży
        if (expenseRate == null || expenseRate.CurrencyCodeKey == tripCurrency)
            return Task.FromResult(amount);

        return Task.FromResult(amount * expenseRate.ExchangeRateValue);
    }

    private List<DebtDetailDto> OptimizeDebts(List<DebtDetailDto> debts)
    {
        // Tworzymy macierz długów między wszystkimi uczestnikami
        var allPersonIds = debts.Select(d => d.FromPersonId)
            .Concat(debts.Select(d => d.ToPersonId))
            .Distinct()
            .ToList();

        var debtMatrix = new Dictionary<string, Dictionary<string, decimal>>();

        // Inicjalizacja macierzy
        foreach (var fromId in allPersonIds)
        {
            debtMatrix[fromId] = new Dictionary<string, decimal>();
            foreach (var toId in allPersonIds)
            {
                debtMatrix[fromId][toId] = 0;
            }
        }

        // Wypełniamy macierz długami
        foreach (var debt in debts)
        {
            debtMatrix[debt.FromPersonId][debt.ToPersonId] += debt.Amount;
        }

        // Optymalizacja: redukujemy wzajemne długi
        foreach (var personA in allPersonIds)
        {
            foreach (var personB in allPersonIds)
            {
                if (personA == personB) continue;

                var debtAB = debtMatrix[personA][personB];
                var debtBA = debtMatrix[personB][personA];

                if (debtAB > 0 && debtBA > 0)
                {
                    var minDebt = Math.Min(debtAB, debtBA);
                    debtMatrix[personA][personB] -= minDebt;
                    debtMatrix[personB][personA] -= minDebt;
                }
            }
        }

        // Konwertujemy macierz z powrotem na listę długów
        var optimized = new List<DebtDetailDto>();
        foreach (var fromId in allPersonIds)
        {
            foreach (var toId in allPersonIds)
            {
                var amount = debtMatrix[fromId][toId];
                if (amount > 0.01m)
                {
                    var fromName = debts.First(d => d.FromPersonId == fromId).FromPersonName;
                    var toName = debts.First(d => d.ToPersonId == toId).ToPersonName;

                    optimized.Add(new DebtDetailDto
                    {
                        FromPersonId = fromId,
                        FromPersonName = fromName,
                        ToPersonId = toId,
                        ToPersonName = toName,
                        Amount = amount
                    });
                }
            }
        }

        return optimized;
    }
}