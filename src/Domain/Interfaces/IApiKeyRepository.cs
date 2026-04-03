using Domain.Entities;

namespace Domain.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetActiveByKeyAsync(
        string apiKey,
        CancellationToken cancellationToken);

    Task<ApiKey> CreateAsync(
        ApiKey apiKey,
        CancellationToken cancellationToken);
}
