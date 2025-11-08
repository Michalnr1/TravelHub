namespace TravelHub.Domain.DTOs;

public class BudgetFilterDto
{
    public int TripId { get; set; }
    public string? PersonId { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeTransfers { get; set; } = true;
    public bool IncludeEstimated { get; set; } = true;
}
