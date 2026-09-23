using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Hrm.IntegrationTests;

public sealed class SystemEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SystemEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:InitializeAuthSupportSchema"] = "false",
                    ["ConnectionStrings:DefaultConnection"] = string.Empty
                })));
    }

    [Fact]
    public async Task SystemInfoReturnsStandardSuccessEnvelope()
    {
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.GetAsync(
            new Uri("/api/v1/system/info", UriKind.Relative),
            cancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("v1", body.RootElement.GetProperty("data").GetProperty("apiVersion").GetString());
    }

    [Fact]
    public async Task LivenessEndpointIsHealthyWithoutDatabaseConfiguration()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/health/live", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpointRejectsAnonymousRequests()
    {
        await using var securedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SaigonRetail.Api",
                    ["Jwt:Audience"] = "SaigonRetail.Web",
                    ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-characters"
                })));
        using var client = securedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(
            new Uri("/api/v1/admin/users", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OffboardingEndpointRejectsAnonymousRequests()
    {
        await using var securedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SaigonRetail.Api",
                    ["Jwt:Audience"] = "SaigonRetail.Web",
                    ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-characters"
                })));
        using var client = securedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsync(
            new Uri("/api/v1/employees/1/offboard", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileEndpointRejectsAnonymousRequests()
    {
        await using var securedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SaigonRetail.Api",
                    ["Jwt:Audience"] = "SaigonRetail.Web",
                    ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-characters"
                })));
        using var client = securedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(
            new Uri("/api/v1/auth/profile", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionStatusEndpointRejectsAnonymousRequests()
    {
        await using var securedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SaigonRetail.Api",
                    ["Jwt:Audience"] = "SaigonRetail.Web",
                    ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-characters"
                })));
        using var client = securedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(
            new Uri("/api/v1/auth/session-status", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
