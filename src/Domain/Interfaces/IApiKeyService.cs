namespace Domain.Interfaces;

public interface IApiKeyService
{
    Task<bool> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken);

    Task<string> GenerateAsync(
        CancellationToken cancellationToken);
}
