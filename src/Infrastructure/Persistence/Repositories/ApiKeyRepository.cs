using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence.Models;
using MongoDB.Driver;

namespace Infrastructure.Persistence.Repositories;

public class ApiKeyRepository(IMongoCollection<ApiKeyDocument> collection) : IApiKeyRepository
{
    public async Task<ApiKey?> GetActiveByKeyAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        var document = await collection
            .Find(item => item.ApiKey == apiKey && !item.Revoked)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : MapToEntity(document);
    }

    public async Task<ApiKey> CreateAsync(
        ApiKey apiKey,
        CancellationToken cancellationToken)
    {
        var document = new ApiKeyDocument
        {
            ApiKey = apiKey.Key,
            CreatedAt = apiKey.CreatedAt,
            Revoked = apiKey.Revoked
        };

        await collection
            .InsertOneAsync(document, cancellationToken: cancellationToken);

        return MapToEntity(document);
    }

    private static ApiKey MapToEntity(
        ApiKeyDocument document)
    {
        return new ApiKey
        {
            Key = document.ApiKey,
            Revoked = document.Revoked
        };
    }
}
