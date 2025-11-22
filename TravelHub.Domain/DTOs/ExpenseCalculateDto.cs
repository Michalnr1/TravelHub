using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class ExpenseCalculationDto
{
    public int ExpenseId { get; set; }
    public decimal OriginalValue { get; set; }
    public CurrencyCode OriginalCurrency { get; set; }
    public CurrencyCode TargetCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedValue { get; set; }

    public decimal AdditionalFee { get; set; }
    public decimal PercentageFee { get; set; }
    public decimal TotalFee { get; set; }
}
