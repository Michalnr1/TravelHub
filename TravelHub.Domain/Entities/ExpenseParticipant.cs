namespace TravelHub.Domain.Entities;

public class ExpenseParticipant
{
    public int ExpenseId { get; set; }
    public required string PersonId { get; set; }

    public decimal Share { get; set; }

    public decimal ActualShareValue { get; set; }

    public Expense? Expense { get; set; }
    public Person? Person { get; set; }
}
