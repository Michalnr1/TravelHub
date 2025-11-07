namespace TravelHub.Domain.DTOs;

public class ParticipantBalanceDto
{
    public string PersonId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal OwesToOthers { get; set; }
    public decimal OwedByOthers { get; set; }
    public decimal NetBalance { get; set; }
}
