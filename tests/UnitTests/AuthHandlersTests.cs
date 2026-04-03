using Application.Commands.Auth.GenerateApiKey;
using Application.Queries.Auth.ValidateApiKey;
using Infrastructure.Services;

namespace UnitTests;

public class AuthHandlersTests
{
    private readonly ApiKeyService _apiKeyService = new();

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnResponse_WhenApiKeyIsValid()
    {
        var handler = new ValidateApiKeyHandler(_apiKeyService);

        var response = await handler.Handle(new ValidateApiKeyQuery("123456"), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Authorized", response!.Message);
    }

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnNull_WhenApiKeyIsInvalid()
    {
        var handler = new ValidateApiKeyHandler(_apiKeyService);

        var response = await handler.Handle(new ValidateApiKeyQuery("invalid"), CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GenerateApiKeyHandler_ShouldReturnGeneratedApiKey()
    {
        var handler = new GenerateApiKeyHandler(_apiKeyService);

        var response = await handler.Handle(new GenerateApiKeyCommand(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.ApiKey));
        Assert.Equal(32, response.ApiKey.Length);
    }
}