using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

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
        var expenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);

        var summary = new TripExpensesSummaryDto();

        foreach (var expense in expenses)
        {
            var expenseCurrency = expense.ExchangeRate?.CurrencyCodeKey ?? tripCurrency;

            if (expenseCurrency == tripCurrency)
            {
                // Ta sama waluta - bez konwersji
                var finalValue = 0m;
                if (expense.IsEstimated)
                {
                    finalValue = expense.EstimatedValue;
                }
                else
                {
                    finalValue = expense.Value;
                }
                finalValue += CalculateFees(expense, expense.Value);
                summary.ExpenseCalculations.Add(new ExpenseCalculationDto
                {
                    ExpenseId = expense.Id,
                    ConvertedValue = finalValue
                });
                continue;
            }

            // Konwersja waluty
            var expenseRate = expense.ExchangeRate;
            if (expenseRate == null)
            {
                // Brak kursów - traktujemy jako 1:1
                var finalValue = 0m;
                if (expense.IsEstimated)
                {
                    finalValue = expense.EstimatedValue;
                }
                else
                {
                    finalValue = expense.Value;
                }
                finalValue += CalculateFees(expense, expense.Value);
                summary.ExpenseCalculations.Add(new ExpenseCalculationDto
                {
                    ExpenseId = expense.Id,
                    ConvertedValue = finalValue
                });
                continue;
            }

            // Oblicz przelicznik
            var conversionRate = expenseRate.ExchangeRateValue;
            var convertedValue = 0m;
            if (expense.IsEstimated)
            {
                convertedValue = conversionRate * expense.EstimatedValue;
            }
            else
            {
                convertedValue = conversionRate * expense.Value;
            }

            // Dolicz opłaty do przeliczonej kwoty
            var finalValueWithFees = convertedValue + CalculateFees(expense, convertedValue);

            summary.ExpenseCalculations.Add(new ExpenseCalculationDto
            {
                ExpenseId = expense.Id,
                ConvertedValue = finalValueWithFees
            });
        }

        return summary;
    }

    //public async Task<decimal> GetTotalExpensesInCurrencyAsync(int tripId, CurrencyCode targetCurrency)
    //{
    //    var summary = await CalculateTripExpensesInTripCurrencyAsync(tripId, targetCurrency);
    //    return summary.TotalExpensesInTripCurrency;
    //}

    public async Task<Expense?> GetExpenseByAccommodationIdAsync(int accommodationId)
    {
        return await _expenseRepository.GetExpenseByAccommodationIdAsync(accommodationId);
    }

    public async Task<BalanceDto> CalculateBalancesAsync(int tripId)
    {
        var expenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);
        expenses = expenses.Where(e => !e.IsEstimated);
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
            var conversionResult = await ConvertExpenseWithFeesAsync(expense, tripCurrency);
            var expenseValueInTripCurrency = conversionResult.ConvertedValue;

            if (expense.TransferredToId == null)
            {
                foreach (var participant in expense.Participants.Where(p => p.PersonId != paidBy))
                {
                    var shareInTripCurrency = expenseValueInTripCurrency * participant.Share;

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

    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(BudgetFilterDto filter)
    {
        var expenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(filter.TripId);
        var trip = await _tripService.GetByIdAsync(filter.TripId) ?? throw new InvalidOperationException($"Trip with id {filter.TripId} not found");

        // Filtruj wydatki
        var filteredExpenses = FilterExpenses(expenses, filter);

        // Konwertuj do waluty podróży
        var expensesInTripCurrency = await ConvertExpensesToTripCurrency(filteredExpenses, trip.CurrencyCode);

        return await CalculateBudgetSummary(expensesInTripCurrency, trip, filter);
    }

    public async Task<Expense?> GetExpenseForSpotAsync(int spotId)
    {
        return await _expenseRepository.GetExpenseForSpotAsync(spotId);
    }

    public async Task<Expense?> GetExpenseForTransportAsync(int transportId)
    {
        return await _expenseRepository.GetExpenseForTransportAsync(transportId);
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

    private async Task<ExpenseConversionResult> ConvertExpenseWithFeesAsync(Expense expense, CurrencyCode tripCurrency)
    {
        var originalValue = expense.Value;
        var expenseCurrency = expense.ExchangeRate?.CurrencyCodeKey ?? tripCurrency;

        // Jeśli ta sama waluta - bez konwersji
        if (expenseCurrency == tripCurrency)
        {
            var finalValue = originalValue + CalculateFees(expense, originalValue);
            return new ExpenseConversionResult
            {
                ConvertedValue = finalValue,
                BaseConvertedValue = originalValue,
                AdditionalFee = expense.AdditionalFee,
                PercentageFee = expense.PercentageFee,
                TotalFee = finalValue - originalValue,
                ExchangeRate = 1m,
                OriginalCurrency = expenseCurrency,
                TargetCurrency = tripCurrency
            };
        }

        // Konwersja waluty
        var convertedValue = await ConvertToTripCurrencyAsync(originalValue, expense.ExchangeRate, tripCurrency);
        var finalValueWithFees = convertedValue + CalculateFees(expense, convertedValue);

        var conversionRate = expense.ExchangeRate?.ExchangeRateValue ?? 1m;

        return new ExpenseConversionResult
        {
            ConvertedValue = finalValueWithFees,
            BaseConvertedValue = convertedValue,
            AdditionalFee = expense.AdditionalFee,
            PercentageFee = expense.PercentageFee,
            TotalFee = finalValueWithFees - convertedValue,
            ExchangeRate = conversionRate,
            OriginalCurrency = expenseCurrency,
            TargetCurrency = tripCurrency
        };
    }

    private decimal CalculateFees(Expense expense, decimal convertedAmount)
    {
        // Opłaty dotyczą tylko wydatków rzeczywistych
        if (expense.IsEstimated)
            return 0m;

        decimal fees = 0m;

        // Opłata stała - dodajemy bezpośrednio
        fees += expense.AdditionalFee;

        // Opłata procentowa - obliczana od przeliczonej kwoty
        if (expense.PercentageFee > 0)
        {
            fees += convertedAmount * (expense.PercentageFee / 100m);
        }

        return fees;
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

    private IEnumerable<Expense> FilterExpenses(IEnumerable<Expense> expenses, BudgetFilterDto filter)
    {
        var query = expenses.AsQueryable();

        // Filtruj po osobie
        if (!string.IsNullOrEmpty(filter.PersonId))
        {
            query = query.Where(e =>
                e.PaidById == filter.PersonId ||
                e.Participants.Any(p => p.PersonId == filter.PersonId) ||
                (!string.IsNullOrEmpty(e.TransferredToId) &&
                (e.PaidById == filter.PersonId || e.TransferredToId == filter.PersonId)));
        }

        // Filtruj po kategorii
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == filter.CategoryId.Value);
        }

        // Filtruj transfery
        if (!filter.IncludeTransfers)
        {
            query = query.Where(e => string.IsNullOrEmpty(e.TransferredToId));
        }

        // Filtruj szacowane
        if (!filter.IncludeEstimated)
        {
            query = query.Where(e => !e.IsEstimated);
        }

        return query.ToList();
    }

    private async Task<List<ExpenseCalculation>> ConvertExpensesToTripCurrency(IEnumerable<Expense> expenses, CurrencyCode tripCurrency)
    {
        var result = new List<ExpenseCalculation>();

        foreach (var expense in expenses)
        {
            ExpenseConversionResult conversionResult;

            if (expense.IsEstimated)
            {
                // Dla wydatków szacunkowych: pomnóż przez multiplier
                var baseValue = expense.EstimatedValue * expense.Multiplier;
                conversionResult = await ConvertExpenseWithFeesAsync(new Expense
                {
                    Name = expense.Name,
                    PaidById = expense.PaidById,
                    Value = baseValue,
                    ExchangeRate = expense.ExchangeRate,
                    IsEstimated = true,
                    AdditionalFee = expense.AdditionalFee,
                    PercentageFee = expense.PercentageFee,
                }, tripCurrency);
            }
            else
            {
                // Dla wydatków rzeczywistych - z opłatami
                conversionResult = await ConvertExpenseWithFeesAsync(expense, tripCurrency);
            }

            result.Add(new ExpenseCalculation
            {
                Expense = expense,
                ConvertedValue = conversionResult.ConvertedValue,
                BaseValue = conversionResult.BaseConvertedValue,
                IsMultiplied = expense.IsEstimated && expense.Multiplier > 1,
                OriginalCurrency = conversionResult.OriginalCurrency,
                TargetCurrency = tripCurrency,
                AdditionalFee = conversionResult.AdditionalFee,
                PercentageFee = conversionResult.PercentageFee,
                TotalFee = conversionResult.TotalFee
            });
        }

        return result;
    }

    private Task<BudgetSummaryDto> CalculateBudgetSummary(List<ExpenseCalculation> expenses, Trip trip, BudgetFilterDto filter)
    {
        var summary = new BudgetSummaryDto
        {
            TripId = trip.Id,
            TripName = trip.Name,
            TripCurrency = trip.CurrencyCode,
            FilterByPersonId = filter.PersonId,
            FilterByCategoryId = filter.CategoryId
        };

        // Pobierz wszystkich unikalnych uczestników wycieczki
        var allParticipants = expenses
            .SelectMany(e => e.Expense.Participants.Select(p => p.Person))
            .Concat(expenses.Select(e => e.Expense.PaidBy))
            .Concat(expenses.Select(e => e.Expense.TransferredTo))
            .Where(p => p != null)
            .DistinctBy(p => p?.Id)
            .ToList();

        // Oblicz podsumowanie kategorii
        summary.CategorySummaries = CalculateCategorySummaries(expenses, filter);

        // Oblicz podsumowanie osób
        summary.PersonSummaries = CalculatePersonSummaries(allParticipants!, expenses, filter);

        // Oblicz sumy ogólne - uwzględniając wszystkie filtry
        summary.TotalActualExpenses = summary.CategorySummaries.Sum(c => c.ActualExpenses);
        summary.TotalEstimatedExpenses = summary.CategorySummaries.Sum(c => c.EstimatedExpenses);
        summary.TotalTransfers = summary.CategorySummaries.Sum(c => c.Transfers);

        var totalTransders = summary.PersonSummaries.Sum(p => p.Transfers);

        // Oblicz procenty
        var totalAll = summary.TotalActualExpenses + summary.TotalTransfers;
        if (totalAll > 0)
        {
            foreach (var category in summary.CategorySummaries)
            {
                category.PercentageOfTotal = (category.Total / summary.TotalActualExpenses) * 100;
            }

            foreach (var person in summary.PersonSummaries)
            {
                person.PercentageOfTotal = (person.Total / summary.TotalActualExpenses + totalTransders) * 100;
            }
        }

        // Uzupełnij nazwy filtrów
        if (!string.IsNullOrEmpty(filter.PersonId))
        {
            var person = allParticipants.FirstOrDefault(p => p?.Id == filter.PersonId);
            summary.FilterByPersonName = person != null ? $"{person.FirstName} {person.LastName}" : filter.PersonId;
        }

        if (filter.CategoryId.HasValue)
        {
            var category = summary.CategorySummaries.FirstOrDefault(c => c.CategoryId == filter.CategoryId.Value);
            summary.FilterByCategoryName = category?.CategoryName ?? filter.CategoryId.ToString();
        }

        return Task.FromResult(summary);
    }

    private List<BudgetCategorySummaryDto> CalculateCategorySummaries(List<ExpenseCalculation> expenses, BudgetFilterDto filter)
    {
        var categorySummaries = new List<BudgetCategorySummaryDto>();

        // Filtruj wydatki dla kategorii - uwzględnij filtr osoby
        var expensesForCategories = expenses;
        if (!string.IsNullOrEmpty(filter.PersonId))
        {
            // Filtruj wydatki tylko do tych, w których uczestniczy wybrana osoba
            expensesForCategories = expenses.Where(e =>
                e.Expense.PaidById == filter.PersonId ||
                e.Expense.Participants.Any(p => p.PersonId == filter.PersonId) ||
                (!string.IsNullOrEmpty(e.Expense.TransferredToId) &&
                (e.Expense.PaidById == filter.PersonId || e.Expense.TransferredToId == filter.PersonId)))
                .ToList();
        }

        // Grupuj po kategoriach - uwzględnij filtr osoby
        var categoryGroups = expensesForCategories.GroupBy(e => new
        {
            Id = e.Expense.CategoryId ?? 0,
            Name = e.Expense.Category?.Name ?? "Uncategorized",
            Color = e.Expense.Category?.Color ?? "#6c757d"
        });

        foreach (var categoryGroup in categoryGroups)
        {
            var categorySummary = CalculateCategorySummary(categoryGroup.Key, categoryGroup.ToList(), filter);
            categorySummaries.Add(categorySummary);
        }

        return categorySummaries;
    }

    private static BudgetCategorySummaryDto CalculateCategorySummary(dynamic categoryKey, List<ExpenseCalculation> categoryExpenses, BudgetFilterDto filter)
    {
        var categorySummary = new BudgetCategorySummaryDto
        {
            CategoryId = categoryKey.Id,
            CategoryName = categoryKey.Name,
            CategoryColor = categoryKey.Color,
            ActualExpenses = 0,
            EstimatedExpenses = 0,
            Transfers = 0
        };

        foreach (var expenseCalc in categoryExpenses)
        {
            var expense = expenseCalc.Expense;

            if (expense.IsEstimated)
            {
                // Dla wydatków szacowanych - tylko jeśli osoba zapłaciła
                if (string.IsNullOrEmpty(filter.PersonId) || expense.PaidById == filter.PersonId)
                {
                    categorySummary.EstimatedExpenses += expenseCalc.ConvertedValue;
                }
            }
            else if (string.IsNullOrEmpty(expense.TransferredToId))
            {
                // Dla zwykłych wydatków - tylko udział wybranej osoby
                if (string.IsNullOrEmpty(filter.PersonId))
                {
                    // Bez filtra - cały wydatek
                    categorySummary.ActualExpenses += expenseCalc.ConvertedValue;
                }
                else
                {
                    // Z filtrem osoby - tylko jej udział
                    var personParticipation = expense.Participants.FirstOrDefault(p => p.PersonId == filter.PersonId);
                    if (personParticipation != null)
                    {
                        var shareInTripCurrency = expenseCalc.ConvertedValue * personParticipation.Share;
                        categorySummary.ActualExpenses += shareInTripCurrency;
                    }
                }
            }
            else
            {
                // Dla transferów
                if (string.IsNullOrEmpty(filter.PersonId))
                {
                    // Bez filtra - cały transfer
                    categorySummary.Transfers += expenseCalc.ConvertedValue;
                }
                else
                {
                    // Z filtrem osoby - uwzględniamy transfer jeśli osoba jest zaangażowana
                    if (expense.PaidById == filter.PersonId)
                    {
                        categorySummary.Transfers += expenseCalc.ConvertedValue; // Wypłata
                    }
                    else if (expense.TransferredToId == filter.PersonId)
                    {
                        categorySummary.Transfers -= expenseCalc.ConvertedValue; // Wpłata (ujemna)
                    }
                }
            }
        }

        return categorySummary;
    }

    private List<BudgetPersonSummaryDto> CalculatePersonSummaries(List<Person> allParticipants, List<ExpenseCalculation> expenses, BudgetFilterDto filter)
    {
        var personSummaries = new List<BudgetPersonSummaryDto>();

        // Filtruj osoby dla podsumowania - uwzględnij filtr kategorii
        var personsToShow = allParticipants;

        if (!string.IsNullOrEmpty(filter.PersonId))
        {
            // Pokaż tylko wybraną osobę
            personsToShow = allParticipants.Where(p => p.Id == filter.PersonId).ToList();
        }
        else if (filter.CategoryId.HasValue)
        {
            // Pokaż tylko osoby, które mają wydatki w wybranej kategorii
            personsToShow = allParticipants.Where(p =>
                expenses.Any(e =>
                    ((e.Expense.PaidById == p.Id || e.Expense.Participants.Any(ep => ep.PersonId == p.Id)) &&
                    e.Expense.CategoryId == filter.CategoryId.Value) ||
                    (!string.IsNullOrEmpty(e.Expense.TransferredToId) &&
                    e.Expense.TransferredToId == p.Id && e.Expense.CategoryId == filter.CategoryId.Value)))
                .ToList();
        }

        // Grupuj po osobach - uwzględnij filtr kategorii
        foreach (var participant in personsToShow)
        {
            var personSummary = CalculatePersonSummary(participant, expenses, filter.CategoryId);
            personSummaries.Add(personSummary);
        }

        return personSummaries;
    }

    private BudgetPersonSummaryDto CalculatePersonSummary(Person? person, List<ExpenseCalculation> expenses, int? categoryFilter)
    {
        var personSummary = new BudgetPersonSummaryDto
        {
            PersonId = person!.Id,
            PersonName = $"{person.FirstName} {person.LastName}",
            ActualExpenses = 0,
            EstimatedExpenses = 0,
            Transfers = 0
        };

        foreach (var expenseCalc in expenses)
        {
            var expense = expenseCalc.Expense;

            // Filtruj po kategorii jeśli jest ustawiona
            if (categoryFilter.HasValue && expense.CategoryId != categoryFilter.Value && string.IsNullOrEmpty(expense.TransferredToId))
                continue;

            // Sprawdź czy osoba jest uczestnikiem tego wydatku
            var personParticipation = expense.Participants.FirstOrDefault(p => p.PersonId == person.Id);

            if (expense.IsEstimated)
            {
                // Dla wydatków szacowanych - osoba płacąca ma cały wydatek
                if (expense.PaidById == person.Id)
                {
                    personSummary.EstimatedExpenses += expenseCalc.ConvertedValue;
                }
            }
            else if (string.IsNullOrEmpty(expense.TransferredToId))
            {
                // Dla zwykłych wydatków - osoba ma tylko swoją część
                if (personParticipation != null)
                {
                    // Konwertuj udział osoby do waluty podróży
                    var shareInTripCurrency = expenseCalc.ConvertedValue * personParticipation.Share;
                    personSummary.ActualExpenses += shareInTripCurrency;
                }
            }
            else
            {
                // Dla transferów
                if (expense.PaidById == person.Id)
                {
                    // Osoba zapłaciła transfer - to jest jej wydatek
                    personSummary.Transfers += expenseCalc.ConvertedValue;
                }
                else if (expense.TransferredToId == person.Id)
                {
                    // Osoba otrzymała transfer - to jest jej przychód (ujemny wydatek)
                    personSummary.Transfers -= expenseCalc.ConvertedValue;
                }
            }
        }

        return personSummary;
    }

    private class ExpenseCalculation
    {
        public Expense Expense { get; set; } = null!;
        public decimal ConvertedValue { get; set; }
        public decimal BaseValue { get; set; } // Wartość bazowa przed konwersją walutową
        public bool IsMultiplied { get; set; }
        public CurrencyCode OriginalCurrency { get; set; }
        public CurrencyCode TargetCurrency { get; set; }

        public decimal AdditionalFee { get; set; }
        public decimal PercentageFee { get; set; }
        public decimal TotalFee { get; set; }
    }

    private class ExpenseConversionResult
    {
        public decimal ConvertedValue { get; set; }
        public decimal BaseConvertedValue { get; set; }
        public decimal AdditionalFee { get; set; }
        public decimal PercentageFee { get; set; }
        public decimal TotalFee { get; set; }
        public decimal ExchangeRate { get; set; }
        public CurrencyCode OriginalCurrency { get; set; }
        public CurrencyCode TargetCurrency { get; set; }
    }
}