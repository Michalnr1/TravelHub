namespace TravelHub.Domain.DTOs;

public class ParticipantBalanceDto
{
    public string PersonId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal NetBalance { get; set; }

    public decimal OwesToOthers => NetBalance < 0 ? Math.Abs(NetBalance) : 0;
    public decimal OwedByOthers => NetBalance > 0 ? NetBalance : 0;
}
