using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class Currency
{
    [Key]
    public required CurrencyCode Key { get; set; }

    [NotMapped]
    public string Name
    {
        get => Key.GetDisplayName();
    }
    public decimal ExchangeRate { get; set; }

    // A currency can be used in many expenses
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
