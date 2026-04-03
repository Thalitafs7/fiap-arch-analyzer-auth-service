namespace Domain.Entities;

public class ApiKey
{
    public string Id { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool Revoked { get; init; }
}
