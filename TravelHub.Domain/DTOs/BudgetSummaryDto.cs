using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class BudgetSummaryDto
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public CurrencyCode TripCurrency { get; set; }

    // Filtry
    public string? FilterByPersonId { get; set; }
    public string? FilterByPersonName { get; set; }
    public int? FilterByCategoryId { get; set; }
    public string? FilterByCategoryName { get; set; }

    // Podsumowanie ogólne
    public decimal TotalActualExpenses { get; set; }
    public decimal TotalEstimatedExpenses { get; set; }
    public decimal TotalTransfers { get; set; }
    public decimal Balance => TotalActualExpenses - TotalEstimatedExpenses;

    // Podsumowanie per kategoria
    public List<BudgetCategorySummaryDto> CategorySummaries { get; set; } = new();

    // Podsumowanie per uczestnik
    public List<BudgetPersonSummaryDto> PersonSummaries { get; set; } = new();
}
