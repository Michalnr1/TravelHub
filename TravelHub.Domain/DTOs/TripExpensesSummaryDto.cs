using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class TripExpensesSummaryDto
{
    public decimal TotalExpensesInTripCurrency { get; set; }
    public CurrencyCode TripCurrency { get; set; }
    public List<ExpenseCalculationDto> ExpenseCalculations { get; set; } = new();
}
