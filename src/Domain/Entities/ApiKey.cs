using Domain.Entities.Base;

namespace Domain.Entities;

public class ApiKey : Entity
{
    public string Key { get; init; } = string.Empty;
    public bool Revoked { get; init; }
}
