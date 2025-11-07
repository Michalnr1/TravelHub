using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Expenses;

public class BalanceViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public CurrencyCode TripCurrency { get; set; }
    public List<ParticipantBalanceViewModel> ParticipantBalances { get; set; } = new List<ParticipantBalanceViewModel>();
    public List<DebtDetailViewModel> DebtDetails { get; set; } = new List<DebtDetailViewModel>();

    public static BalanceViewModel FromDto(BalanceDto dto)
    {
        return new BalanceViewModel
        {
            TripId = dto.TripId,
            TripName = dto.TripName,
            TripCurrency = dto.TripCurrency,
            ParticipantBalances = dto.ParticipantBalances.Select(pb => new ParticipantBalanceViewModel
            {
                PersonId = pb.PersonId,
                FullName = pb.FullName,
                OwesToOthers = pb.OwesToOthers,
                OwedByOthers = pb.OwedByOthers,
                NetBalance = pb.NetBalance
            }).ToList(),
            DebtDetails = dto.DebtDetails.Select(dd => new DebtDetailViewModel
            {
                FromPersonId = dd.FromPersonId,
                FromPersonName = dd.FromPersonName,
                ToPersonId = dd.ToPersonId,
                ToPersonName = dd.ToPersonName,
                Amount = dd.Amount
            }).ToList()
        };
    }
}

public class ParticipantBalanceViewModel
{
    public string PersonId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal OwesToOthers { get; set; }
    public decimal OwedByOthers { get; set; }
    public decimal NetBalance { get; set; }

    public string FormattedOwesToOthers => $"{OwesToOthers:N2}";
    public string FormattedOwedByOthers => $"{OwedByOthers:N2}";
    public string FormattedNetBalance => $"{NetBalance:N2}";

    public string NetBalanceClass => NetBalance switch
    {
        > 0 => "text-success",
        < 0 => "text-danger",
        _ => "text-muted"
    };
}

public class DebtDetailViewModel
{
    public string FromPersonId { get; set; } = string.Empty;
    public string FromPersonName { get; set; } = string.Empty;
    public string ToPersonId { get; set; } = string.Empty;
    public string ToPersonName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"{Amount:N2}";
}