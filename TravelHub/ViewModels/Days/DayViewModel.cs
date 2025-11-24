using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.ViewModels.Days;

public class DayDetailsViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public int TripId { get; set; }
    public Trip? Trip { get; set; }
    public int? AccommodationId { get; set; }
    public Accommodation? Accommodation { get; set; }

    public List<ActivityDetailsViewModel> Activities { get; set; } = new List<ActivityDetailsViewModel>();
    public List<ActivityDetailsViewModel> AvailableActivities { get; set; } = new List<ActivityDetailsViewModel>();
}