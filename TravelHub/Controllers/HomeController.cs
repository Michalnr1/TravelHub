using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Web.Models;

namespace TravelHub.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;

            _configuration = configuration;
            _apiKey = _configuration["ApiKeys:GoogleApiKey"]!;

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API Key not found in configuration.");
            }
        }

        public IActionResult Index()
        {
            ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
