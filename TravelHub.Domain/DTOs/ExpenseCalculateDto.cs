using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class ExpenseCalculationDto
{
    public int ExpenseId { get; set; }
    public decimal ConvertedValue { get; set; }
}
