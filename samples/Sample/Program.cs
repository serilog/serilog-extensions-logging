using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddSerilog();
                });

            var serviceProvider = services.BuildServiceProvider();
            // getting the logger using the class's name is conventional
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var startTime = DateTimeOffset.UtcNow;
            logger.LogInformation(1, "Started at {StartTime} and 0x{Hello:X} is hex of 42", startTime, 42);

            try
            {
                throw new Exception("Boom!");
            }
            catch (Exception ex)
            {
                logger.LogCritical("Unexpected critical error starting application", ex);
                logger.Log(LogLevel.Critical, 0, "Unexpected critical error", ex, null);
                // This write should not log anything
                logger.Log<object>(LogLevel.Critical, 0, null, null, null);
                logger.LogError("Unexpected error", ex);
                logger.LogWarning("Unexpected warning", ex);
            }

            using (logger.BeginScope("Main"))
            {
                logger.LogInformation("Waiting for user input");
                var key = Console.Read();
                logger.LogInformation("User pressed {@KeyInfo}", new { Key = key, KeyChar = (char)key });
            }

            var endTime = DateTimeOffset.UtcNow;
            logger.LogInformation(2, "Stopping at {StopTime}", endTime);

            logger.LogInformation("Stopping");

            logger.LogInformation(Environment.NewLine);
            logger.LogInformation("{Result,-10:l}{StartTime,15:l}{EndTime,15:l}{Duration,15:l}", "RESULT", "START TIME", "END TIME", "DURATION(ms)");
            logger.LogInformation("{Result,-10:l}{StartTime,15:l}{EndTime,15:l}{Duration,15:l}", "------", "----- ----", "--- ----", "------------");
            logger.LogInformation("{Result,-10:l}{StartTime,15:mm:s tt}{EndTime,15:mm:s tt}{Duration,15}", "SUCCESS", startTime, endTime, (endTime - startTime).TotalMilliseconds);
        }
    }
}
