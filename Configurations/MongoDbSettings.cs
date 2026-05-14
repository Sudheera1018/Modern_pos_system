namespace ModernPosSystem.Configurations;

public class MongoDbSettings
{
    public const string SectionName = "MongoDbSettings";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "ModernPosSystemDb";
}
