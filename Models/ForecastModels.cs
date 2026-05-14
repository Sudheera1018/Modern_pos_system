namespace ModernPosSystem.Models;

[BsonCollection("Forecasts")]
public class ForecastDocument : BaseEntity
{
    public string StoreId { get; set; } = string.Empty;

    public int ForecastStoreCode { get; set; }

    public string ProductId { get; set; } = string.Empty;

    public int ForecastItemCode { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public DateTime ForecastGeneratedAt { get; set; } = DateTime.UtcNow;

    public DateTime ForecastStartDate { get; set; } = DateTime.UtcNow.Date;

    public string GeneratedSource { get; set; } = "Manual";

    public decimal PredictedNextDaySales { get; set; }

    public decimal PredictedNext7DaysTotal { get; set; }

    public List<ForecastDailyItem> DailyForecasts { get; set; } = [];

    public decimal CurrentStock { get; set; }

    public decimal SafetyStock { get; set; }

    public decimal ReorderSuggestedQuantity { get; set; }

    public ReorderStatus ReorderStatus { get; set; } = ReorderStatus.Healthy;

    public bool IsSlowMoving { get; set; }

    public decimal AverageRecentDailySales { get; set; }

    public string ForecastModelName { get; set; } = string.Empty;

    public string ForecastVersion { get; set; } = string.Empty;

    public string ConfidenceLabel { get; set; } = "Low";

    public int InputHistoryDays { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string SlowMovingExplanation { get; set; } = string.Empty;

    public string ReorderExplanation { get; set; } = string.Empty;
}

public class ForecastDailyItem
{
    public DateTime Date { get; set; }

    public decimal PredictedQuantity { get; set; }
}
