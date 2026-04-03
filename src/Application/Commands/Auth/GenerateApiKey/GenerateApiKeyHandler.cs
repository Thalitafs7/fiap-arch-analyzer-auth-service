using Application.Common.Exceptions;
using Application.Common.Handlers;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace Application.Commands.Auth.GenerateApiKey;

public class GenerateApiKeyHandler(
    IApiKeyRepository apiKeyRepository,
    IInternalKeyValidator internalKeyValidator,
    ILogService<GenerateApiKeyHandler> logService) : HandlerBase<GenerateApiKeyHandler>(logService), IRequestHandler<GenerateApiKeyCommand, GenerateApiKeyResponse>
{
    public async Task<GenerateApiKeyResponse> Handle(
        GenerateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        const string method = nameof(Handle);

        try
        {
            LogInicio(method, command);

            if (!internalKeyValidator.IsValid(command.XInternalKey))
                throw new UnauthorizedException();

            var apiKey = new ApiKey
            {
                Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            };

            var createdApiKey = await apiKeyRepository
                .CreateAsync(apiKey, cancellationToken);

            var result = new GenerateApiKeyResponse(createdApiKey.Key);

            LogFim(method, result);

            return result;
        }
        catch (Exception ex)
        {
            LogErro(method, ex);
            throw;
        }
    }
}
