using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Hosting;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly IConverter _converter;
    private readonly IWebHostEnvironment _environment;

    public PdfService(IConverter converter, IWebHostEnvironment environment)
    {
        _converter = converter;
        _environment = environment;
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent, string fileName = "export.pdf")
    {
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                DPI = 300,
                Margins = new MarginSettings {
                    Top = 15,
                    Bottom = 15,
                    Left = 10,
                    Right = 10
                },
                DocumentTitle = fileName
            },
            Objects = {
                new ObjectSettings()
                {
                    HtmlContent = htmlContent,
                    WebSettings = {
                        DefaultEncoding = "utf-8",
                        LoadImages = true,
                        EnableJavascript = false, // Wyłącz JavaScript dla lepszej kompatybilności
                        PrintMediaType = true
                    },
                    HeaderSettings = {
                        FontSize = 9,
                        Right = "Strona [page] z [toPage]",
                        Line = false,
                        Spacing = 5
                    },
                    FooterSettings = {
                        FontSize = 9,
                        Center = $"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}",
                        Line = false,
                        Spacing = 5
                    }
                }
            }
        };

        return await Task.Run(() => _converter.Convert(doc));
    }
}