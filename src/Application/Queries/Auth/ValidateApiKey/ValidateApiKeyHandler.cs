using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Auth.ValidateApiKey;

public class ValidateApiKeyHandler(IApiKeyService apiKeyService) : IRequestHandler<ValidateApiKeyQuery, ValidateApiKeyResponse?>
{
    public async Task<ValidateApiKeyResponse?> Handle(
        ValidateApiKeyQuery request,
        CancellationToken cancellationToken)
    {
        var isValid = await apiKeyService
            .ValidateAsync(request.ApiKey, cancellationToken);

        return isValid ? new ValidateApiKeyResponse("Authorized") : null;
    }
}