using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class BalanceDto
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public CurrencyCode TripCurrency { get; set; }
    public List<ParticipantBalanceDto> ParticipantBalances { get; set; } = new List<ParticipantBalanceDto>();
    public List<DebtDetailDto> DebtDetails { get; set; } = new List<DebtDetailDto>();
}
