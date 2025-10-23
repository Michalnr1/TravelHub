using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Expenses;

public class ExpenseViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string PaidByName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }
}

public class ExpenseDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string PaidByName { get; set; }
    public string? CategoryName { get; set; }
    public required string CurrencyName { get; set; }
    public string? CurrencyKey { get; set; }
    public List<string> ParticipantNames { get; set; } = new List<string>();
    public int TripId { get; set; }
    public string? TripName { get; set; }
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

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [Display(Name = "Currency")]
    public string CurrencyKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Display(Name = "Participants")]
    public List<string> SelectedParticipants { get; set; } = new List<string>();

    // Select lists
    public List<CurrencySelectItem> Currencies { get; set; } = new List<CurrencySelectItem>();
    public List<CategorySelectItem> Categories { get; set; } = new List<CategorySelectItem>();
    public List<PersonSelectItem> People { get; set; } = new List<PersonSelectItem>();
    public List<PersonSelectItem> AllPeople { get; set; } = new List<PersonSelectItem>();
}

public class CurrencySelectItem
{
    public required string Key { get; set; }
    public required string Name { get; set; }
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