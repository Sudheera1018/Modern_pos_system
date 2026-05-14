namespace ModernPosSystem.Configurations;

public class ForecastingOptions
{
    public const string SectionName = "Forecasting";

    public string DefaultStoreId { get; set; } = "main-store";

    public int DefaultForecastStoreCode { get; set; } = 1;

    public int MinimumHistoryDays { get; set; } = 60;

    public int HighConfidenceDays { get; set; } = 60;

    public int MediumConfidenceDays { get; set; } = 30;

    public decimal SlowMovingMaxNext7Days { get; set; } = 5;

    public decimal SlowMovingHighStockThreshold { get; set; } = 20;

    public decimal DefaultSafetyStock { get; set; } = 5;

    public string ModelName { get; set; } = "XGBoost POS Forecasting Model";

    public string ModelVersion { get; set; } = "1.0.0";

    public bool AutoRunEnabled { get; set; } = true;

    public string AutoRunLocalTime { get; set; } = "08:00:00";

    public int AutoRunCheckIntervalMinutes { get; set; } = 5;

    public bool AutoRunOnStartupCatchUp { get; set; } = true;
}
