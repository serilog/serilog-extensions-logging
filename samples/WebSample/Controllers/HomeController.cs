using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace WebSample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            Log.Information("Hello from the Index!");

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

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
