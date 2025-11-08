namespace TravelHub.Domain.DTOs;

public class BudgetPersonSummaryDto
{
    public string PersonId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public decimal ActualExpenses { get; set; }
    public decimal EstimatedExpenses { get; set; }
    public decimal Transfers { get; set; }
    public decimal Total => ActualExpenses + Transfers;
    public decimal Balance => ActualExpenses - EstimatedExpenses;

    // Procentowo w stosunku do całości
    public decimal PercentageOfTotal { get; set; }
}
