namespace TravelHub.Domain.DTOs;

public class ExpenseCreateUpdateDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string PaidById { get; set; }
    public int? CategoryId { get; set; }
    public int ExchangeRateId { get; set; }
    public List<ParticipantShareDto> Shares { get; set; } = new List<ParticipantShareDto>();
}
