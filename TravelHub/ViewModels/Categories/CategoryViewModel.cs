using System.ComponentModel.DataAnnotations;

namespace TravelHub.Web.ViewModels.Categories;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa jest wymagana")]
    [StringLength(100, ErrorMessage = "Nazwa nie może być dłuższa niż 100 znaków")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kolor jest wymagany")]
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Nieprawidłowy format koloru (np. #FF5733)")]
    [Display(Name = "Kolor (hex)")]
    public string Color { get; set; } = "#000000";
}