namespace TravelHub.Domain.DTOs;

public class PublicTripSearchCriteriaDto
{
    public string? SearchTerm { get; set; }
    public string? CountryCode { get; set; }
    public int? MinDays { get; set; }
    public int? MaxDays { get; set; }

    public bool HasFilters =>
        !string.IsNullOrEmpty(SearchTerm) ||
        !string.IsNullOrEmpty(CountryCode) ||
        MinDays.HasValue ||
        MaxDays.HasValue;
}
