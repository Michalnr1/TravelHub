using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Expenses;

namespace TravelHub.Web.ViewModels.Accommodations;

public class AccommodationViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    // public decimal Cost { get; set; }
    public string? CategoryName { get; set; }
    public string? DayName { get; set; }
    public ICollection<Day> Days { get; set; } = new List<Day>();
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DurationString => $"{(CheckOut - CheckIn).Days} nights";
}

public class AccommodationDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public int Order { get; set; }
    public string? CategoryName { get; set; }
    public string? DayName { get; set; }
    public ICollection<Day> Days { get; set; } = new List<Day>();
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    // public decimal Cost { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal CheckInTime { get; set; }
    public string CheckInTimeString { get; set; } = "00:00";
    public decimal CheckOutTime { get; set; }
    public string CheckOutTimeString { get; set; } = "00:00";
    public string DurationString => $"{(CheckOut - CheckIn).Days} nights";
    public int TripId { get; set; }
    public string? TripName { get; set; }
}

public class AccommodationCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string? Description { get; set; }

    // Duration i Order nie są edytowalne przez użytkownika
    public decimal Duration { get; set; } = 0;
    public int Order { get; set; } = 0;

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    // DayId jest ustawiane automatycznie, nie jest edytowalne przez użytkownika
    public int? DayId { get; set; }
    public ICollection<Day> DaysForAccommodation { get; set; } = new List<Day>();

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    //[Required(ErrorMessage = "Cost is required")]
    //[Range(0, double.MaxValue, ErrorMessage = "Cost must be greater than or equal to 0")]
    //public decimal Cost { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    [Display(Name = "Check-in date")]
    [DataType(DataType.Date)]
    public DateTime CheckIn { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Check-out date is required")]
    [Display(Name = "Check-out date")]
    [DataType(DataType.Date)]
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

    public decimal CheckInTime { get; set; } = 14.0m;

    [Required(ErrorMessage = "Check-in time is required")]
    [Display(Name = "Check-in time")]
    [RegularExpression(@"^([0-9]{1,2}):([0-5][0-9])$", ErrorMessage = "Please enter Check-in time in format HH:MM")]
    public string CheckInTimeString { get; set; } = "00:00";

    public decimal CheckOutTime { get; set; } = 10.0m;

    [Required(ErrorMessage = "Check-out time is required")]
    [Display(Name = "Check-out time")]
    [RegularExpression(@"^([0-9]{1,2}):([0-5][0-9])$", ErrorMessage = "Please enter check-out time in format HH:MM")]
    public string CheckOutTimeString { get; set; } = "00:00";

    // Sekcja Expense
    [Display(Name = "Estimated cost")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
    public decimal? ExpenseValue { get; set; }

    [Display(Name = "Currency")]
    public CurrencyCode? ExpenseCurrencyCode { get; set; } = CurrencyCode.PLN;

    [Display(Name = "Exchange rate (to base)")]
    [Range(0.000001, (double)decimal.MaxValue, ErrorMessage = "Exchange Rate must be greater than 0")]
    public decimal? ExpenseExchangeRateValue { get; set; } = 1.0M;

    // Select lists
    public List<CategorySelectItem> Categories { get; set; } = new List<CategorySelectItem>();
    public List<TripSelectItem> Trips { get; set; } = new List<TripSelectItem>();
    public List<DaySelectItem> Days { get; set; } = new List<DaySelectItem>();

    // Select lists dla Expense
    public List<CurrencySelectGroupItem> CurrenciesGroups { get; set; } = new List<CurrencySelectGroupItem>();
}

public class CategorySelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class TripSelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class DaySelectItem
{
    public int Id { get; set; }
    public required string DisplayName { get; set; }
}