using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Squidlr.Api.IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITestOutputHelper? TestOutputHelper { get; set; }

    public ApiWebApplicationFactory()
    {
        ClientOptions.AllowAutoRedirect = false;
        ClientOptions.HandleCookies = false;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var output = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(TestOutputHelper, LogEventLevel.Information, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        builder.UseSerilog(output);

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("APPLICATION__APIKEY", "foobar");
        builder.UseEnvironment("Development");
    }
}