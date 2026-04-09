using Application.Common.Constants;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentApiKeyService(IHttpContextAccessor httpContextAccessor) : ICurrentApiKeyService
{
    public string? ApiKey => httpContextAccessor.HttpContext?
        .Request
        .Headers[HeaderNames.ApiKey]
        .FirstOrDefault();
}
