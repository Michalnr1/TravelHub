namespace TravelHub.Domain.DTOs;

public class BudgetCategorySummaryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal ActualExpenses { get; set; }
    public decimal EstimatedExpenses { get; set; }
    public decimal Transfers { get; set; }
    public decimal Total => ActualExpenses;
    public decimal Balance => ActualExpenses - EstimatedExpenses;

    // Procentowo w stosunku do całości
    public decimal PercentageOfTotal { get; set; }
}
