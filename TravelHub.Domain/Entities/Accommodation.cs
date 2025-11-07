namespace TravelHub.Domain.Entities;

public class Accommodation : Spot
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal CheckInTime { get; set; }
    public decimal CheckOutTime { get; set; }

    public ICollection<Day> Days { get; set; } = new List<Day>();
}
