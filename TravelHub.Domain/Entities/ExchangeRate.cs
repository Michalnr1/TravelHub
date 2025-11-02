using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class ExchangeRate
{
    [Key]
    public int Id { get; set; }

    public required CurrencyCode CurrencyCodeKey { get; set; }

    [NotMapped]
    public string Name
    {
        get => CurrencyCodeKey.GetDisplayName();
    }

    public decimal ExchangeRateValue { get; set; }

    public int TripId { get; set; }

    public Trip? Trip { get; set; }

    // A currency entry can be used in many expenses
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
