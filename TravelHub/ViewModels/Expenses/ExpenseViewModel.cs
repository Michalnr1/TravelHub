using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.ViewModels.Expenses;

public class ExpenseViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public decimal EstimatedValue { get; set; }
    public required string PaidByName { get; set; }
    public string? TransferredToName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }

    public CurrencyCode CurrencyCode { get; set; }
    public decimal ExchangeRateValue { get; set; }

    public bool IsEstimated { get; set; }
    public int Multiplier { get; set; } = 1;
    public int? SpotId { get; set; }
    public string? SpotName { get; set; }
    public int? TransportId { get; set; }
    public string? TransportName { get; set; }

    // Obliczona wartość w walucie podróży
    public decimal ConvertedValue { get; set; }
    public decimal ConvertedEstimatedValue => EstimatedValue * ExchangeRateValue;

    // Formatowane wartości
    public string FormattedConvertedValue => $"{ConvertedValue:N2}";
    public string FormattedConvertedEstimatedValue => $"{ConvertedEstimatedValue:N2}";

    public string StatusBadge
    {
        get
        {
            if (IsEstimated)
                return "<span class='badge bg-warning text-dark'><i class='fas fa-clock'></i> Estimated</span>";
            if (!string.IsNullOrEmpty(TransferredToName))
                return "<span class='badge bg-info'><i class='fas fa-exchange-alt'></i> Transfer</span>";
            return "<span class='badge bg-success'><i class='fas fa-check'></i> Actual</span>";
        }
    }
}

public class ExpenseDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public decimal EstimatedValue { get; set; }
    public required string PaidByName { get; set; }
    public string? TransferredToName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }
    public CurrencyCode? CurrencyKey { get; set; }
    public List<ExpenseParticipantDetail> Participants { get; set; } = new List<ExpenseParticipantDetail>();
    public int TripId { get; set; }
    public string? TripName { get; set; }
    public CurrencyCode TripCurrency { get; set; }

    public bool IsEstimated { get; set; }
    public int Multiplier { get; set; } = 1;
    public int? SpotId { get; set; }
    public string? SpotName { get; set; }
    public int? TransportId { get; set; }
    public string? TransportName { get; set; }
    public decimal ExchangeRateValue { get; set; } = 1.0m;

    // Obliczona wartość w walucie podróży
    public decimal ConvertedValue => Value * ExchangeRateValue;
    public string FormattedConvertedValue => $"{ConvertedValue:N2} {TripCurrency}";
    public decimal ConvertedEstimatedValue => EstimatedValue * ExchangeRateValue;
    public string FormattedConvertedEstimatedValue => $"{ConvertedEstimatedValue:N2} {TripCurrency}";

    public string GetExpenseTypeBadge()
    {
        if (IsEstimated)
            return "<span class='badge bg-warning text-dark'><i class='fas fa-clock'></i> Estimated</span>";
        if (!string.IsNullOrEmpty(TransferredToName))
            return "<span class='badge bg-info'><i class='fas fa-exchange-alt'></i> Transfer</span>";
        return "<span class='badge bg-success'><i class='fas fa-check'></i> Actual</span>";
    }

    public string GetExpenseTypeDescription()
    {
        if (IsEstimated)
            return "This is a planned/estimated cost for budgeting purposes.";
        if (!string.IsNullOrEmpty(TransferredToName))
            return "This is a money transfer between participants.";
        return "This expense has been actually paid.";
    }

    public bool HasRelatedEntity => SpotId.HasValue || TransportId.HasValue;

    public string GetRelatedEntityName()
    {
        if (SpotId.HasValue) return SpotName ?? "Related Spot";
        if (TransportId.HasValue) return TransportName ?? "Related Transport";
        return "";
    }

    public string GetRelatedEntityType()
    {
        if (SpotId.HasValue) return "Spot";
        if (TransportId.HasValue) return "Transport";
        return "";
    }

    public int? GetRelatedEntityId()
    {
        if (SpotId.HasValue) return SpotId;
        if (TransportId.HasValue) return TransportId;
        return null;
    }
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
    [Range(0.00, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
    public decimal Value { get; set; }

    [Display(Name = "Estimated Value")]
    [Range(0.00, double.MaxValue, ErrorMessage = "Estimated value must be greater than 0")]
    public decimal EstimatedValue { get; set; }

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
    
    [Display(Name = "Is Estimated")]
    public bool IsEstimated { get; set; } = false;

    [Display(Name = "Multiplier (for estimated expenses)")]
    [Range(1, int.MaxValue, ErrorMessage = "Multiplier must be at least 1")]
    public int Multiplier { get; set; } = 1;

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }
    public CurrencyCode TripCurrency { get; set; }

    [Display(Name = "Related Spot")]
    public int? SpotId { get; set; }

    [Display(Name = "Related Transport")]
    public int? TransportId { get; set; }

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
    public List<SpotSelectItem> Spots { get; set; } = new List<SpotSelectItem>();
    public List<TransportationTypeSelectItem> Transports { get; set; } = new List<TransportationTypeSelectItem>();
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