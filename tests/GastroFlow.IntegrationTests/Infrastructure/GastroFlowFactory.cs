using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace GastroFlow.IntegrationTests.Infrastructure;

// Boots the full ASP.NET pipeline against a real PostgreSQL container.
// Overrides connection string and JWT settings so no external config is needed.
public sealed class GastroFlowFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"]               = "gastroflow-integration-test-secret-key!!",
                ["Jwt:Issuer"]            = "GastroFlow",
                ["Jwt:Audience"]          = "GastroFlowUsers",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });
    }
}
