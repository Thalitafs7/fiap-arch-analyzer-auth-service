using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace Application.Commands.Auth.GenerateApiKey;

public class GenerateApiKeyHandler(IApiKeyRepository apiKeyRepository) : IRequestHandler<GenerateApiKeyCommand, GenerateApiKeyResponse>
{
    public async Task<GenerateApiKeyResponse> Handle(
        GenerateApiKeyCommand request, 
        CancellationToken cancellationToken)
    {
        var apiKey = new ApiKey
        {
            Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        };

        var createdApiKey = await apiKeyRepository
            .CreateAsync(apiKey, cancellationToken);

        return new GenerateApiKeyResponse(createdApiKey.Key);
    }
}
