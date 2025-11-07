namespace TravelHub.Domain.DTOs;

public class DebtDetailDto
{
    public string FromPersonId { get; set; } = string.Empty;
    public string FromPersonName { get; set; } = string.Empty;
    public string ToPersonId { get; set; } = string.Empty;
    public string ToPersonName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
