using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

// Teraz dziedziczy z serwisu generycznego
public class ExpenseService : GenericService<Expense>, IExpenseService
{
    private readonly IExpenseRepository _expenseRepository; // Specyficzne repozytorium do metod customowych

    public ExpenseService(IExpenseRepository expenseRepository)
        : base(expenseRepository) // Przekazujemy repozytorium do serwisu generycznego
    {
        _expenseRepository = expenseRepository;
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
        //if (participantIds != null && participantIds.Any())
        //{
        //    var participantsList = participantIds.ToList();
        //    var totalParticipantsCount = participantsList.Count;

        //    if (entity.Value > 0)
        //    {
        //        var defaultSharePercentage = Math.Round(1m / totalParticipantsCount, 3);
        //        var actualSharePerPerson = Math.Round(entity.Value / totalParticipantsCount, 2);

        //        entity.Participants = participantsList
        //            .Select(personId => new ExpenseParticipant
        //            {
        //                PersonId = personId,
        //                Share = defaultSharePercentage,
        //                ActualShareValue = actualSharePerPerson
        //            })
        //            .ToList();
        //    }
        //    else
        //    {
        //        entity.Participants = participantsList
        //            .Select(personId => new ExpenseParticipant
        //            {
        //                PersonId = personId,
        //                Share = Math.Round(1m / totalParticipantsCount, 3),
        //                ActualShareValue = 0m
        //            })
        //            .ToList();
        //    }
        //}

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
        //// 1. Zidentyfikuj istniejące i nowe identyfikatory
        //var existingParticipantLinks = existingExpense.Participants ?? new List<ExpenseParticipant>();
        //var currentParticipantIds = existingParticipantLinks.Select(ep => ep.PersonId).ToList();
        //var newParticipantIdsList = newParticipantIds.ToList();

        //// 2. Uczestnicy do usunięcia
        //var participantsToRemove = existingParticipantLinks
        //    .Where(ep => !newParticipantIdsList.Contains(ep.PersonId))
        //    .ToList();

        //// 3. Uczestnicy do dodania
        //var participantsToAddIds = newParticipantIdsList
        //    .Except(currentParticipantIds)
        //    .ToList();

        //// 4. Wykonaj faktyczne usunięcia
        //foreach (var link in participantsToRemove)
        //{
        //    existingExpense.Participants?.Remove(link);
        //}

        //// 5. Ustaw logikę obliczania udziałów dla nowych i istniejących
        //var totalParticipantsCount = newParticipantIdsList.Count;
        //var defaultSharePercentage = totalParticipantsCount > 0 ? Math.Round(1m / totalParticipantsCount, 3) : 0m;
        //var actualSharePerPerson = totalParticipantsCount > 0 ? Math.Round(existingExpense.Value / totalParticipantsCount, 2) : 0m;

        //// 6. Dodaj nowych uczestników
        //foreach (var personId in participantsToAddIds)
        //{
        //    existingExpense.Participants?.Add(new ExpenseParticipant
        //    {
        //        PersonId = personId,
        //        ExpenseId = existingExpense.Id,
        //        Share = defaultSharePercentage,
        //        ActualShareValue = actualSharePerPerson
        //    });
        //}

        //// 7. Aktualizuj pola Share/ActualShareValue dla wszystkich pozostałych
        //foreach (var link in existingExpense.Participants!)
        //{
        //    link.Share = defaultSharePercentage;
        //    link.ActualShareValue = actualSharePerPerson;
        //}

        //// 8. Wywołaj generyczną aktualizację
        //await _expenseRepository.UpdateAsync(existingExpense);

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
}