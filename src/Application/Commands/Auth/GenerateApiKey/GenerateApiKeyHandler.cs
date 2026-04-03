using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.Auth.GenerateApiKey;

public class GenerateApiKeyHandler(IApiKeyService apiKeyService) : IRequestHandler<GenerateApiKeyCommand, GenerateApiKeyResponse>
{
    public async Task<GenerateApiKeyResponse> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var apiKey = await apiKeyService
            .GenerateAsync(cancellationToken);

        return new GenerateApiKeyResponse(apiKey);
    }
}
