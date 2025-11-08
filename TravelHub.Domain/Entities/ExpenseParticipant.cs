namespace TravelHub.Domain.Entities;

public class ExpenseParticipant
{
    public int ExpenseId { get; set; }
    public required string PersonId { get; set; }
    // part of the expense from 0 to 1
    public decimal Share { get; set; }
    // actual value of the part
    public decimal ActualShareValue { get; set; }

    public Expense? Expense { get; set; }
    public Person? Person { get; set; }
}
