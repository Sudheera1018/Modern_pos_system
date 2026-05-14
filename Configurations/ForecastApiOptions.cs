namespace ModernPosSystem.Configurations;

public class ForecastApiOptions
{
    public const string SectionName = "ForecastApi";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    public int TimeoutSeconds { get; set; } = 15;
}
