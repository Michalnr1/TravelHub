using System;
using System.ComponentModel.DataAnnotations;

namespace TravelHub.Web.Validation;

public class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minimumAge;
    private readonly int _maximumAge;

    public MinimumAgeAttribute(int minimumAge, int maximumAge = 120)
    {
        _minimumAge = minimumAge;
        _maximumAge = maximumAge;
        ErrorMessage = $"You must be between {_minimumAge} and {_maximumAge} years old.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime date)
        {
            return ValidationResult.Success; // Let [Required] handle nulls
        }

        var today = DateTime.Today;
        var age = today.Year - date.Year;

        if (date > today.AddYears(-age))
            age--;

        if (age < _minimumAge || age > _maximumAge)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}

