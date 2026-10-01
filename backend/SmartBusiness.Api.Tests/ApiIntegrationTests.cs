using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SmartBusiness.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<DesignTimeApiFactory>
{
    private readonly HttpClient client;

    public ApiIntegrationTests(DesignTimeApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_is_available_through_the_http_pipeline()
    {
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_product_endpoint_rejects_anonymous_requests()
    {
        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Development_configuration_contains_a_valid_jwt_key()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SmartBusiness.Api"))
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var jwtKey = configuration["Jwt:Key"];

        Assert.False(string.IsNullOrWhiteSpace(jwtKey));
        Assert.True(jwtKey.Length >= 32, "The JWT key must be at least 32 characters long.");
    }
}

public sealed class DesignTimeApiFactory : WebApplicationFactory<Program>
{
    public DesignTimeApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-key-that-is-at-least-32-characters");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "integration-test-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "integration-test-audience");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("DesignTime");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "integration-test-key-that-is-at-least-32-characters",
            ["Jwt:Issuer"] = "integration-test-issuer",
            ["Jwt:Audience"] = "integration-test-audience",
            ["ConnectionStrings:SmartBusiness"] = "Server=unused;Database=unused;User Id=unused;Password=unused;"
        }));
    }
}