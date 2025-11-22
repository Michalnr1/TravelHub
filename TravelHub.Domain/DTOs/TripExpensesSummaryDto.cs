using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

// DTO used only to store summed value of all expenses for quick view in trip details
public class TripExpensesSummaryDto
{
    public List<ExpenseCalculationDto> ExpenseCalculations { get; set; } = new();
}
