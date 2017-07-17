using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;


namespace Serilog
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> with Serilog configuration methods.
    /// </summary>
    public static class SerilogLoggerServicesExtensions
    {
        /// <summary>
        /// Add Serilog to the logging pipeline.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add services to.</param>
        /// <param name="logger">The Serilog logger; if not supplied, the static <see cref="Serilog.Log"/> will be used.</param>
        /// <param name="dispose">When true, dispose <paramref name="logger"/> when the framework disposes the provider. If the
        /// logger is not specified but <paramref name="dispose"/> is true, the <see cref="Log.CloseAndFlush()"/> method will be
        /// called on the static <see cref="Log"/> class instead.</param>
        /// <returns>The logger factory.</returns>
        public static IServiceCollection AddSerilog(this IServiceCollection services, Serilog.ILogger logger = null, bool dispose = false)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddLogging(builder => builder.AddProvider(new SerilogLoggerProvider(logger, dispose)));

            return services;
        }
    }
}
