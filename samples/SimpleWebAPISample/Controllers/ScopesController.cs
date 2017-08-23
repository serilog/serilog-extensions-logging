using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.Logging;

namespace SimpleWebAPISample.Controllers
{
    [Route("api/[controller]")]
    public class ScopesController : Controller
    {
        ILogger<ScopesController> _logger;

        public ScopesController(ILogger<ScopesController> logger)
        {
            _logger = logger;
        }

        // GET api/scopes
        [HttpGet]
        public IEnumerable<string> Get()
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

            return new string[] { "value1", "value2" };
        }
    }
}
