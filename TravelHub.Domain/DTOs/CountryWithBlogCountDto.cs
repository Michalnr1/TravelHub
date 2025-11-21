namespace TravelHub.Domain.DTOs;

public class CountryWithBlogCountDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BlogCount { get; set; }
}