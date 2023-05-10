#if NET7_0

using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class ApiApprovalTests
{
    [Fact]
    public void PublicApi_Should_Not_Change_Unintentionally()
    {
        var assembly = typeof(LoggerSinkConfigurationExtensions).Assembly;
        var publicApi = assembly.GeneratePublicApi(
            new()
            {
                IncludeAssemblyAttributes = false,
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" },
            });

        publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((_, _, fileType, fileExtension) => $"{assembly.GetName().Name!}.{fileType}.{fileExtension}"));
    }
}

#endif
