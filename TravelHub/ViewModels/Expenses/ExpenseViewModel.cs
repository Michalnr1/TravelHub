using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravelHub.Web.ViewModels.Expenses;

public class ExpenseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Value { get; set; }
    public string PaidByName { get; set; }
    public string CategoryName { get; set; }
    public string CurrencyName { get; set; }
}

public class ExpenseDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Value { get; set; }
    public string PaidByName { get; set; }
    public string CategoryName { get; set; }
    public string CurrencyName { get; set; }
    public string CurrencyKey { get; set; }
    public List<string> ParticipantNames { get; set; } = new List<string>();
}

public class ExpenseCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Value is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
    public decimal Value { get; set; }

    [Required(ErrorMessage = "Paid by is required")]
    [Display(Name = "Paid By")]
    public string PaidById { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [Display(Name = "Currency")]
    public string CurrencyKey { get; set; }

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
    public string Key { get; set; }
    public string Name { get; set; }
}

public class CategorySelectItem
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class PersonSelectItem
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}