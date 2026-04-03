using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Auth.ValidateApiKey;

public class ValidateApiKeyHandler(IApiKeyRepository apiKeyRepository) : IRequestHandler<ValidateApiKeyQuery, ValidateApiKeyResponse?>
{
    public async Task<ValidateApiKeyResponse?> Handle(
        ValidateApiKeyQuery request,
        CancellationToken cancellationToken)
    {
        var apiKey = await apiKeyRepository
            .GetActiveByKeyAsync(request.ApiKey, cancellationToken);

        return apiKey is not null ? new ValidateApiKeyResponse("Authorized") : null;
    }
}
