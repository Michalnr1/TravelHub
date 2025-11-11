using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace TravelHub.Infrastructure.Services
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent, string fileName = "export.pdf");
    }

    public class PdfService : IPdfService
    {
        private readonly IConverter _converter;

        public PdfService(IConverter converter)
        {
            _converter = converter;
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
                },
                Objects = {
                    new ObjectSettings()
                    {
                        HtmlContent = htmlContent,
                        WebSettings = {
                            DefaultEncoding = "utf-8",
                            LoadImages = true,
                            EnableJavascript = true,
                            PrintMediaType = true
                        },
                    }
                }
            };
            return _converter.Convert(doc);
        }
    }
}
