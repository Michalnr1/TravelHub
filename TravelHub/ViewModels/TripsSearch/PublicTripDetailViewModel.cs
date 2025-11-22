using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Transports;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.ViewModels.TripsSearch;

public class PublicTripDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPrivate { get; set; } = true;
    public CurrencyCode CurrencyCode { get; set; }
    public string? OwnerName { get; set; }
    public bool IsOwnerPublic { get; set; }

    // Collections
    public List<DayViewModel> Days { get; set; } = new();
    public List<ActivityViewModel> Activities { get; set; } = new();
    public List<SpotDetailsViewModel> Spots { get; set; } = new();
    public List<TransportViewModel> Transports { get; set; } = new();
    public List<AccommodationViewModel> Accommodations { get; set; } = new();
    public List<PublicExpenseViewModel> Expenses { get; set; } = new();

    // Counts for display
    public int ActivitiesCount => Activities.Count;
    public int SpotsCount => Spots.Count;
    public int TransportsCount => Transports.Count;
    public int AccommodationsCount => Accommodations.Count;
    public int ExpensesCount => Expenses.Count;
    public int ParticipantsCount { get; set; }
    public int EstimatedExpensesCount => Expenses.Count;
    public decimal TotalExpenses { get; set; } // Suma wydatków rzeczywistych
    public decimal EstimatedExpensesTotal { get; set; } // Suma wydatków szacowanych
    public decimal CombinedTotal => TotalExpenses + EstimatedExpensesTotal;

    // Helper properties
    public int Duration => (EndDate - StartDate).Days + 1;
    public string DateRange => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    public int NormalDaysCount => Days.Count(d => !d.IsGroup);
    public int GroupsCount => Days.Count(d => d.IsGroup);

    // Formatowane wartości
    public string FormattedTotalExpenses => $"{TotalExpenses:N2} {CurrencyCode}";
    public string FormattedEstimatedExpensesTotal => $"{EstimatedExpensesTotal:N2} {CurrencyCode}";
    public string FormattedCombinedTotal => $"{CombinedTotal:N2} {CurrencyCode}";
    public string OwnerDisplay => IsOwnerPublic ? OwnerName! : "Private User";
}

public class PublicExpenseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal EstimatedValue { get; set; }
    public string? CategoryName { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public CurrencyCode CurrencyCode { get; set; }
    public decimal ExchangeRateValue { get; set; }
    public decimal ConvertedValue { get; set; }
    public bool IsEstimated { get; set; }
    public int Multiplier { get; set; } = 1;
    public int? SpotId { get; set; }
    public string? SpotName { get; set; }
    public int? TransportId { get; set; }
    public string? TransportName { get; set; }
    public bool IsTransfer { get; set; }

    public decimal ConvertedEstimatedValue => EstimatedValue * ExchangeRateValue;
}
