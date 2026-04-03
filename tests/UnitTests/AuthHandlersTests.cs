using Application.Commands.Auth.GenerateApiKey;
using Application.Queries.Auth.ValidateApiKey;
using Domain.Entities;
using Domain.Interfaces;

namespace UnitTests;

public class AuthHandlersTests
{
    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnResponse_WhenApiKeyIsValid()
    {
        var repository = new FakeApiKeyRepository([
            new ApiKey
            {
                Id = Guid.NewGuid().ToString(),
                Key = "valid-api-key",
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            }
        ]);

        var handler = new ValidateApiKeyHandler(repository);

        var response = await handler.Handle(new ValidateApiKeyQuery("valid-api-key"), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Authorized", response!.Message);
    }

    [Fact]
    public async Task ValidateApiKeyHandler_ShouldReturnNull_WhenApiKeyIsRevoked()
    {
        var repository = new FakeApiKeyRepository([
            new ApiKey
            {
                Id = Guid.NewGuid().ToString(),
                Key = "revoked-api-key",
                CreatedAt = DateTime.UtcNow,
                Revoked = true
            }
        ]);

        var handler = new ValidateApiKeyHandler(repository);

        var response = await handler.Handle(new ValidateApiKeyQuery("revoked-api-key"), CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GenerateApiKeyHandler_ShouldPersistAndReturnGeneratedApiKey()
    {
        var repository = new FakeApiKeyRepository([]);
        var handler = new GenerateApiKeyHandler(repository);

        var response = await handler.Handle(new GenerateApiKeyCommand(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.ApiKey));
        Assert.Equal(64, response.ApiKey.Length);
        Assert.Single(repository.Items);
        Assert.Equal(response.ApiKey, repository.Items[0].Key);
        Assert.False(repository.Items[0].Revoked);
    }

    private sealed class FakeApiKeyRepository(List<ApiKey> items) : IApiKeyRepository
    {
        public List<ApiKey> Items { get; } = items;

        public Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken)
        {
            var createdApiKey = new ApiKey
            {
                Id = Guid.NewGuid().ToString(),
                Key = apiKey.Key,
                CreatedAt = apiKey.CreatedAt,
                Revoked = apiKey.Revoked
            };

            Items.Add(createdApiKey);
            return Task.FromResult(createdApiKey);
        }

        public Task<ApiKey?> GetActiveByKeyAsync(string apiKey, CancellationToken cancellationToken)
        {
            var item = Items.FirstOrDefault(current => current.Key == apiKey && !current.Revoked);
            return Task.FromResult(item);
        }
    }
}
