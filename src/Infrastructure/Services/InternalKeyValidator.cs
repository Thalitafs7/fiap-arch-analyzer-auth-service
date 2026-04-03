using Application.Common.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class InternalKeyValidator(IOptions<SecuritySettings> securitySettings) : IInternalKeyValidator
{
    public bool IsValid(string internalKey)
    {
        var configuredKey = securitySettings.Value.XInternalKey;
        return !string.IsNullOrWhiteSpace(configuredKey)
            && string.Equals(internalKey, configuredKey, StringComparison.Ordinal);
    }
}
