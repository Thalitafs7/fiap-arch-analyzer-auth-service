using Domain.Interfaces;

namespace Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private const string DefaultApiKey = "123456";

    //TODO: implementar consulta no banco
    public Task<bool> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        var isValid = string.Equals(apiKey, DefaultApiKey, StringComparison.Ordinal);

        return Task.FromResult(isValid);
    }

    public Task<string> GenerateAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}
