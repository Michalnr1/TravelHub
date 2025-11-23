using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.BudgetSummary;

public class BudgetSummaryViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public CurrencyCode TripCurrency { get; set; }
    public bool IncludeTransfers { get; set; } = true;
    public bool IncludeEstimated { get; set; } = true;

    // Filtry
    public string? FilterByPersonId { get; set; }
    public string? FilterByPersonName { get; set; }
    public int? FilterByCategoryId { get; set; }
    public string? FilterByCategoryName { get; set; }

    // Podsumowanie ogólne
    public decimal TotalActualExpenses { get; set; }
    public decimal TotalEstimatedExpenses { get; set; }
    public decimal TotalTransfers { get; set; }
    public decimal Balance { get; set; }

    // Podsumowanie per kategoria
    public List<BudgetCategorySummaryViewModel> CategorySummaries { get; set; } = new();

    // Podsumowanie per uczestnik
    public List<BudgetPersonSummaryViewModel> PersonSummaries { get; set; } = new();

    // Listy do filtrów
    public List<PersonFilterItem> AvailablePeople { get; set; } = new();
    public List<CategoryFilterItem> AvailableCategories { get; set; } = new();

    // Listy do SelectList
    public List<SelectListItem> AvailablePeopleSelectList { get; set; } = new();
    public List<SelectListItem> AvailableCategoriesSelectList { get; set; } = new();

    public static BudgetSummaryViewModel FromDto(BudgetSummaryDto dto)
    {
        return new BudgetSummaryViewModel
        {
            TripId = dto.TripId,
            TripName = dto.TripName,
            TripCurrency = dto.TripCurrency,
            FilterByPersonId = dto.FilterByPersonId,
            FilterByPersonName = dto.FilterByPersonName,
            FilterByCategoryId = dto.FilterByCategoryId,
            FilterByCategoryName = dto.FilterByCategoryName,
            TotalActualExpenses = dto.TotalActualExpenses,
            TotalEstimatedExpenses = dto.TotalEstimatedExpenses,
            TotalTransfers = dto.TotalTransfers,
            Balance = dto.Balance,
            CategorySummaries = dto.CategorySummaries.Select(BudgetCategorySummaryViewModel.FromDto).ToList(),
            PersonSummaries = dto.PersonSummaries.Select(BudgetPersonSummaryViewModel.FromDto).ToList()
        };
    }
}

public class BudgetCategorySummaryViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal ActualExpenses { get; set; }
    public decimal EstimatedExpenses { get; set; }
    public decimal Transfers { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public decimal PercentageOfTotal { get; set; }

    public static BudgetCategorySummaryViewModel FromDto(BudgetCategorySummaryDto dto)
    {
        return new BudgetCategorySummaryViewModel
        {
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            CategoryColor = dto.CategoryColor,
            ActualExpenses = dto.ActualExpenses,
            EstimatedExpenses = dto.EstimatedExpenses,
            Transfers = dto.Transfers,
            Total = dto.Total,
            Balance = dto.Balance,
            PercentageOfTotal = dto.PercentageOfTotal
        };
    }
}

public class BudgetPersonSummaryViewModel
{
    public string PersonId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public decimal ActualExpenses { get; set; }
    public decimal EstimatedExpenses { get; set; }
    public decimal Transfers { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public decimal PercentageOfTotal { get; set; }

    public static BudgetPersonSummaryViewModel FromDto(BudgetPersonSummaryDto dto)
    {
        return new BudgetPersonSummaryViewModel
        {
            PersonId = dto.PersonId,
            PersonName = dto.PersonName,
            ActualExpenses = dto.ActualExpenses,
            EstimatedExpenses = dto.EstimatedExpenses,
            Transfers = dto.Transfers,
            Total = dto.Total,
            Balance = dto.Balance,
            PercentageOfTotal = dto.PercentageOfTotal
        };
    }
}

public class BudgetFilterViewModel
{
    public int TripId { get; set; }

    [FromForm(Name = "FilterByPersonId")]
    public string? PersonId { get; set; }

    [FromForm(Name = "FilterByCategoryId")]
    public int? CategoryId { get; set; }
    public bool IncludeTransfers { get; set; } = true;
    public bool IncludeEstimated { get; set; } = true;
}

public class PublicBudgetFilterViewModel
{
    public int TripId { get; set; }

    [FromForm(Name = "FilterByCategoryId")]
    public int? CategoryId { get; set; }

    public bool IncludeEstimated { get; set; } = true;
}

public class PersonFilterItem
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class CategoryFilterItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
