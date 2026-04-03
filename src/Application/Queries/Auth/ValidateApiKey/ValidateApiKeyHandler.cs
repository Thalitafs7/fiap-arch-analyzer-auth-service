using Application.Common.Handlers;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.Auth.ValidateApiKey;

public class ValidateApiKeyHandler(
    IApiKeyRepository apiKeyRepository,
    ILogService<ValidateApiKeyHandler> logService) : HandlerBase<ValidateApiKeyHandler>(logService), IRequestHandler<ValidateApiKeyQuery, ValidateApiKeyResponse?>
{
    public async Task<ValidateApiKeyResponse?> Handle(
        ValidateApiKeyQuery command,
        CancellationToken cancellationToken)
    {
        const string method = nameof(Handle);

        try
        {
            LogInicio(method, command);

            var apiKey = await apiKeyRepository
                .GetActiveByKeyAsync(command.ApiKey, cancellationToken);

            var result = apiKey is not null ? new ValidateApiKeyResponse("Authorized") : null;

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
