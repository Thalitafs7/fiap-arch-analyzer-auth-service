using System.Net;
using System.Net.Http.Json;
using Application.Common.Constants;
using Domain.Entities;
using IntegrationTests.Fixtures;

namespace IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
    }

    [Fact]
    public async Task Validate_ShouldReturnOk_WhenApiKeyExists()
    {
        _factory.Repository.Seed(new ApiKey
        {
            Key = "valid-key",
            Revoked = false
        });
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderNames.ApiKey, "valid-key");

        var response = await client.PostAsync("/api/auth/validate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["message"].Should().Be("Authorized");
    }

    [Fact]
    public async Task Validate_ShouldReturnUnauthorized_WhenHeaderIsMissing()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/validate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateApiKey_ShouldReturnOk_WhenInternalKeyIsValid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-internal-key", "admin123");

        var response = await client.PostAsync("/api/auth/apikey", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().ContainKey("apiKey");
        _factory.Repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateApiKey_ShouldReturnUnauthorized_WhenInternalKeyIsInvalid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-internal-key", "invalid");

        var response = await client.PostAsync("/api/auth/apikey", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
