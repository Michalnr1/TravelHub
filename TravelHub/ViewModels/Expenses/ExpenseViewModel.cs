using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Expenses;

public class ExpenseViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string PaidByName { get; set; }
    public string? TransferredToName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }

    public CurrencyCode CurrencyCode { get; set; }
    public decimal ExchangeRateValue { get; set; }

    // Obliczona wartość w walucie podróży
    public decimal ConvertedValue { get; set; }

    // Formatowane wartości
    public string FormattedConvertedValue => $"{ConvertedValue:N2}";
}

public class ExpenseDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string PaidByName { get; set; }
    public string? TransferredToName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }
    public CurrencyCode? CurrencyKey { get; set; }
    public List<ExpenseParticipantDetail> Participants { get; set; } = new List<ExpenseParticipantDetail>();
    public int TripId { get; set; }
    public string? TripName { get; set; }
}

public class ExpenseParticipantDetail
{
    public required string FullName { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal SharePercentage { get; set; } // W procentach 0-100
}

public class ExpenseCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Value is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
    public decimal Value { get; set; }

    [Required(ErrorMessage = "Paid by is required")]
    [Display(Name = "Paid By")]
    public string PaidById { get; set; } = string.Empty;

    [Display(Name = "Transfer To")]
    public string? TransferredToId { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [Display(Name = "Currency")]
    public CurrencyCode SelectedCurrencyCode { get; set; } = CurrencyCode.PLN;

    [Required(ErrorMessage = "Exchange Rate is required")]
    [Range(0.000001, (double)decimal.MaxValue, ErrorMessage = "Exchange Rate must be greater than 0")]
    [Display(Name = "Exchange Rate (to Base)")]
    public decimal ExchangeRateValue { get; set; } = 1.0M;

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    // Helper flag
    public bool IsTransfer => !string.IsNullOrEmpty(TransferredToId);

    [Display(Name = "Participants")]
    public List<string> SelectedParticipants { get; set; } = new List<string>();
    public List<ParticipantShareViewModel> ParticipantsShares { get; set; } = new List<ParticipantShareViewModel>();

    // Select lists
    public List<CurrencySelectGroupItem> CurrenciesGroups { get; set; } = new List<CurrencySelectGroupItem>();
    public List<CategorySelectItem> Categories { get; set; } = new List<CategorySelectItem>();
    public List<PersonSelectItem> People { get; set; } = new List<PersonSelectItem>();
    public List<PersonSelectItem> AllPeople { get; set; } = new List<PersonSelectItem>();
}

public class ParticipantShareViewModel
{
    public required string PersonId { get; set; }
    public required string FullName { get; set; }

    public decimal Share { get; set; } = 0.000m;

    public decimal ActualShareValue { get; set; } = 0.00m;

    public int ShareType { get; set; } = 1; // ie. 0: Default/Equal, 1: Amount, 2: Percent
}

public class CurrencySelectGroupItem
{
    public required CurrencyCode Key { get; set; }
    public required string Name { get; set; }
    public decimal ExchangeRate { get; set; }
    public bool IsUsed { get; set; }

    public string DropdownText
    {
        get => IsUsed
            ? $"{Key}, {ExchangeRate:F4}"
            : $"{Key} ({Name})";
    }
}

public class CategorySelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class PersonSelectItem
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
}