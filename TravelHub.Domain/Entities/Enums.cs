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
    // Europe
    [Display(Name = "Polish Złoty")]
    PLN,

    [Display(Name = "Euro")]
    EUR,

    [Display(Name = "British Pound Sterling")]
    GBP,

    [Display(Name = "Swiss Franc")]
    CHF,

    // Americas
    [Display(Name = "US Dollar")]
    USD,

    [Display(Name = "Canadian Dollar")]
    CAD,

    [Display(Name = "Mexican Peso")]
    MXN,

    // Asia/Oceania
    [Display(Name = "Japanese Yen")]
    JPY,

    [Display(Name = "Chinese Yuan Renminbi")]
    CNY,

    [Display(Name = "Indian Rupee")]
    INR,

    [Display(Name = "Australian Dollar")]
    AUD,

    [Display(Name = "Hong Kong Dollar")]
    HKD,

    // Other Major Currencies
    [Display(Name = "Russian Ruble")]
    RUB,

    [Display(Name = "Brazilian Real")]
    BRL,

    [Display(Name = "South African Rand")]
    ZAR
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
