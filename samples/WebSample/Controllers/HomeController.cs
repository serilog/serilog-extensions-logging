using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.Logging;

namespace WebSample.Controllers
{
    public class HomeController : Controller
    {
        ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Before");

            using (_logger.BeginScope("Some name"))
            using (_logger.BeginScope(42))
            using (_logger.BeginScope("Formatted {WithValue}", 12345))
            using (_logger.BeginScope(new Dictionary<string, object> { ["ViaDictionary"] = 100 }))
            {
                _logger.LogInformation("Hello from the Index!");
            }

            _logger.LogInformation("After");
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            // Directly through Serilog
            Log.Information("This is a handler for {Path}", Request.Path);

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
