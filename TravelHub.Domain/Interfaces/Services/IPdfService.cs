namespace TravelHub.Domain.Interfaces.Services;

public interface IPdfService
{
    Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent, string fileName = "export.pdf");
}
