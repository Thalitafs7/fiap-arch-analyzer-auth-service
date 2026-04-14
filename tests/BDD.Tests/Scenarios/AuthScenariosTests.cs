using System.Net;
using System.Net.Http.Json;
using Application.Common.Constants;
using BDD.Tests.Hooks;
using Domain.Entities;

namespace BDD.Tests.Scenarios;

public class AuthScenariosTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public AuthScenariosTests(TestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    [Fact]
    public async Task Cenario_ValidarApiKeyExistente_DeveRetornarSucesso()
    {
        _fixture.Repository.Seed(new ApiKey
        {
            Key = "valid-key",
            Revoked = false
        });
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderNames.ApiKey, "valid-key");

        var response = await client.PostAsync("/api/auth/validate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["message"].Should().Be("Authorized");
    }

    [Fact]
    public async Task Cenario_GerarApiKeyComChaveInternaValida_DeveRetornarNovaChave()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("x-internal-key", "admin123");

        var response = await client.PostAsync("/api/auth/apikey", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().ContainKey("apiKey");
    }
}
