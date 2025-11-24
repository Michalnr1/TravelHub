using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Expenses;

namespace TravelHub.Web.ViewModels.Trips;

public class FlightSearchViewModel
{
    public int TripId { get; set; }
    public string? FromAirportCode { get; set; }
    public string? ToAirportCode { get; set; }
    public CurrencyCode? DefaultCurrencyCode { get; set; }
    public List<CurrencySelectGroupItem> Currencies { get; set; } = new List<CurrencySelectGroupItem>();
    public DateTime DefaultDate { get; set; }
    public int Participants { get; set; }
}
