namespace Infrastructure.Options;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string ApiKeysCollectionName { get; init; } = "apiKeys";
}
