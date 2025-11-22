using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class ActivityService : GenericService<Activity>, IActivityService
{
    private readonly IActivityRepository _activityRepository;

    public ActivityService(IActivityRepository activityRepository)
        : base(activityRepository)
    {
        _activityRepository = activityRepository;
    }

    public async Task<IReadOnlyList<Activity>> GetOrderedDailyActivitiesAsync(int dayId)
    {
        return await _activityRepository.GetActivitiesByDayIdAsync(dayId);
    }

    public async Task<decimal> CalculateDailyActivityDurationAsync(int dayId)
    {
        var activities = await _activityRepository.GetActivitiesByDayIdAsync(dayId);

        if (activities == null || !activities.Any())
        {
            return 0;
        }

        return activities.Sum(a => a.Duration);
    }

    public async Task ReorderActivitiesAsync(int dayId, List<(int activityId, int newOrder)> orderUpdates)
    {
        var activities = await _activityRepository.GetActivitiesByDayIdAsync(dayId);

        foreach (var update in orderUpdates)
        {
            var activityToUpdate = activities.FirstOrDefault(a => a.Id == update.activityId);
            if (activityToUpdate != null && activityToUpdate.DayId == dayId)
            {
                activityToUpdate.Order = update.newOrder;
                await UpdateAsync(activityToUpdate);
            }
        }
    }

    public async Task<IEnumerable<Activity>> GetAllWithDetailsAsync()
    {
        return await _activityRepository.GetAllWithDetailsAsync();
    }

    public async Task<IEnumerable<Activity>> GetTripActivitiesWithDetailsAsync(int tripId)
    {
        return await _activityRepository.GetTripActivitiesWithDetailsAsync(tripId);
    }

    public async Task<bool> UserOwnsActivityAsync(int activityId, string userId)
    {
        var activity = await GetByIdAsync(activityId);
        return activity?.Trip?.PersonId == userId;
    }

    public async Task<Activity?> GetActivityWithTripAndParticipantsAsync(int activityId)
    {
        return await _activityRepository.GetByIdWithTripAndParticipantsAsync(activityId);
    }

    public async Task AddChecklistItemAsync(int activityId, string item)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId) ?? throw new KeyNotFoundException();
        activity.Checklist ??= new Checklist();
        activity.Checklist.AddItem(item, false);

        await _activityRepository.UpdateAsync(activity);
    }

    public async Task ToggleChecklistItemAsync(int activityId, string itemTitle)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId) ?? throw new KeyNotFoundException();
        var current = activity.Checklist ?? new Checklist();

        var copy = new Checklist();
        foreach (var it in current.Items)
            copy.Items.Add(new ChecklistItem { Title = it.Title, IsCompleted = it.IsCompleted, AssignedParticipantId = it.AssignedParticipantId, AssignedParticipantName = it.AssignedParticipantName });

        var target = copy.Items.FirstOrDefault(x => x.Title == itemTitle);
        if (target != null)
            target.IsCompleted = !target.IsCompleted;
        else
            copy.Items.Add(new ChecklistItem { Title = itemTitle, IsCompleted = true });

        activity.Checklist = copy;
        await _activityRepository.UpdateAsync(activity);
    }

    public async Task AssignParticipantToItemAsync(int activityId, string itemTitle, string? participantId)
    {
        var activity = await _activityRepository.GetByIdWithTripAndParticipantsAsync(activityId) ?? throw new KeyNotFoundException();

        var current = activity.Checklist ?? new Checklist();
        var copy = new Checklist();
        foreach (var it in current.Items)
            copy.Items.Add(new ChecklistItem { Title = it.Title, IsCompleted = it.IsCompleted, AssignedParticipantId = it.AssignedParticipantId, AssignedParticipantName = it.AssignedParticipantName });

        var target = copy.Items.FirstOrDefault(i => i.Title == itemTitle);
        if (target == null) throw new KeyNotFoundException();

        if (string.IsNullOrWhiteSpace(participantId))
        {
            // unassign
            target.AssignedParticipantId = null;
            target.AssignedParticipantName = null;
        }
        else
        {
            // verify participant belongs to parent trip
            var trip = activity.Trip ?? throw new InvalidOperationException("Activity is not attached to a Trip.");
            var participant = trip.Participants?.FirstOrDefault(p =>
                ((p.Id != 0 && p.Id.ToString() == participantId) || (p.PersonId != null && p.PersonId == participantId))
            );

            if (participant == null)
                throw new InvalidOperationException("Participant does not belong to the parent Trip.");

            target.AssignedParticipantId = participantId;
            target.AssignedParticipantName = participant.Person != null
                ? $"{participant.Person.FirstName} {participant.Person.LastName}"
                : (participant.PersonId ?? participantId);
        }

        activity.Checklist = copy;
        await _activityRepository.UpdateAsync(activity);
    }

    public async Task RemoveChecklistItemAsync(int activityId, string itemTitle)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId) ?? throw new KeyNotFoundException();
        var current = activity.Checklist ?? new Checklist();
        if (!current.Items.Any()) return;

        var copy = new Checklist();
        foreach (var it in current.Items)
            copy.Items.Add(new ChecklistItem { Title = it.Title, IsCompleted = it.IsCompleted, AssignedParticipantId = it.AssignedParticipantId, AssignedParticipantName = it.AssignedParticipantName });

        copy.Items.RemoveAll(x => x.Title == itemTitle);
        activity.Checklist = copy;
        await _activityRepository.UpdateAsync(activity);
    }

    public async Task RenameChecklistItemAsync(int activityId, string oldTitle, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) throw new ArgumentException("New title required", nameof(newTitle));

        var activity = await _activityRepository.GetByIdAsync(activityId) ?? throw new KeyNotFoundException();
        var current = activity.Checklist ?? new Checklist();
        var copy = new Checklist();
        foreach (var it in current.Items)
            copy.Items.Add(new ChecklistItem { Title = it.Title, IsCompleted = it.IsCompleted, AssignedParticipantId = it.AssignedParticipantId, AssignedParticipantName = it.AssignedParticipantName });

        var target = copy.Items.FirstOrDefault(x => x.Title == oldTitle);
        if (target == null) throw new KeyNotFoundException();
        if (copy.Items.Any(x => x.Title == newTitle)) throw new InvalidOperationException("Duplicate title");

        target.Title = newTitle;
        activity.Checklist = copy;
        await _activityRepository.UpdateAsync(activity);
    }
}