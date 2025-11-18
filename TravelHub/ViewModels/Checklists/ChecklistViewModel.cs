using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Checklists
{
    public class EditChecklistItemViewModel
    {
        public int TripId { get; set; }

        // original item key (old name)
        public string OldItem { get; set; } = string.Empty;

        // new title provided by user
        public string NewItem { get; set; } = string.Empty;
    }

    public class ChecklistPageViewModel
    {
        public int TripId { get; set; }
        public Checklist Checklist { get; set; } = new();
        public List<ParticipantVm> Participants { get; set; } = new();
    }

    public class ParticipantVm { public string? Id { get; set; } public string DisplayName { get; set; } = ""; }

}
