using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TravelHub.Domain.Entities;

public enum Status
{
    Planning,
    Finished
}

public enum TransportationType
{
    Car,
    Motorcycle,
    Plane,
    Ship,
    Ferry,
    Taxi,
    Bus,
    Walk
}

public enum Rating
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5
}

public enum CurrencyCode
{
    [Display(Name = "Polski Złoty")]
    PLN,

    [Display(Name = "Dolar Amerykański")]
    USD,

    [Display(Name = "Euro")]
    EUR,

    [Display(Name = "Funt Szterling")]
    GBP
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        // 1. Get enum value
        var member = enumValue.GetType().GetMember(enumValue.ToString()).First();

        // 2. Get [Display] value
        var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();

        // 3. Return DisplayName
        return displayAttribute?.GetName() ?? enumValue.ToString();
    }
}
