using System.Text.Json.Serialization;

namespace ModernPosSystem.DTOs;

public class DailySalesHistoryDto
{
    public DateTime Date { get; set; }

    public decimal Quantity { get; set; }
}

public class ForecastRequestDto
{
    public string StoreId { get; set; } = string.Empty;

    public int ForecastStoreCode { get; set; }

    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public int ForecastItemCode { get; set; }

    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public DateTime ForecastDate { get; set; }

    public List<DailySalesHistoryDto> SalesHistory { get; set; } = [];

    public bool Debug { get; set; }
}

public class ForecastResponseDto
{
    public string StoreId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public DateTime ForecastStartDate { get; set; }

    public decimal PredictedNextDaySales { get; set; }

    public decimal PredictedNext7DaysTotal { get; set; }

    public List<ForecastDailyResponseDto> DailyForecasts { get; set; } = [];

    public string ConfidenceLabel { get; set; } = "Low";

    public int InputHistoryDays { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public class ForecastDailyResponseDto
{
    public DateTime Date { get; set; }

    public decimal PredictedQuantity { get; set; }
}

public class ProductForecastResultDto
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string StoreId { get; set; } = string.Empty;

    public int ForecastStoreCode { get; set; }

    public int ForecastItemCode { get; set; }

    public DateTime ForecastStartDate { get; set; }

    public DateTime ForecastGeneratedAt { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal SafetyStock { get; set; }

    public decimal AverageRecentDailySales { get; set; }

    public decimal PredictedNextDaySales { get; set; }

    public decimal PredictedNext7DaysTotal { get; set; }

    public List<ForecastDailyResponseDto> DailyForecasts { get; set; } = [];

    public decimal ReorderSuggestedQuantity { get; set; }

    public string ReorderStatus { get; set; } = string.Empty;

    public bool IsSlowMoving { get; set; }

    public string ConfidenceLabel { get; set; } = "Low";

    public int InputHistoryDays { get; set; }

    public string ForecastModelName { get; set; } = string.Empty;

    public string ForecastVersion { get; set; } = string.Empty;

    public List<string> Warnings { get; set; } = [];
}

public class ReorderSuggestionDto
{
    public decimal SafetyStock { get; set; }

    public decimal RequiredStock { get; set; }

    public decimal SuggestedQuantity { get; set; }

    public string ReorderStatus { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;
}

public class ForecastQueryDto
{
    public string StoreId { get; set; } = string.Empty;

    public DateTime ForecastDate { get; set; } = DateTime.UtcNow.Date;

    public string? ProductId { get; set; }

    public string? CategoryId { get; set; }

    public string? ReorderStatus { get; set; }

    public bool SlowMovingOnly { get; set; }
}

public class ForecastGenerationRequestDto
{
    public string StoreId { get; set; } = string.Empty;

    public DateTime ForecastDate { get; set; } = DateTime.UtcNow.Date;

    public string? ProductId { get; set; }

    public string? CategoryId { get; set; }

    public bool Debug { get; set; }

    public string GenerationSource { get; set; } = Helpers.ForecastGenerationSources.Manual;
}

public class ForecastGenerationSummaryDto
{
    public int RequestedProducts { get; set; }

    public int GeneratedProducts { get; set; }

    public int SkippedProducts { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public class ForecastApiHealthDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "down";

    [JsonPropertyName("model_loaded")]
    public bool ModelLoaded { get; set; }

    [JsonPropertyName("feature_columns_loaded")]
    public bool FeatureColumnsLoaded { get; set; }
}

public class SalesHistoryPointDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class NextDayPredictionRequestDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("forecast_date")]
    public string ForecastDate { get; set; } = string.Empty;

    [JsonPropertyName("sales_history")]
    public List<SalesHistoryPointDto> SalesHistory { get; set; } = [];

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

public class NextDayPredictionResponseDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("forecast_date")]
    public string ForecastDate { get; set; } = string.Empty;

    [JsonPropertyName("predicted_next_day_sales")]
    public decimal PredictedNextDaySales { get; set; }

    [JsonPropertyName("input_history_days")]
    public int InputHistoryDays { get; set; }

    [JsonPropertyName("confidence_label")]
    public string ConfidenceLabel { get; set; } = "Low";

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];
}

public class Next7DaysPredictionRequestDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("sales_history")]
    public List<SalesHistoryPointDto> SalesHistory { get; set; } = [];

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

public class DailyForecastPointDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("predicted_quantity")]
    public decimal PredictedQuantity { get; set; }
}

public class Next7DaysPredictionResponseDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("daily_forecasts")]
    public List<DailyForecastPointDto> DailyForecasts { get; set; } = [];

    [JsonPropertyName("predicted_next_7_days_total")]
    public decimal PredictedNext7DaysTotal { get; set; }

    [JsonPropertyName("input_history_days")]
    public int InputHistoryDays { get; set; }

    [JsonPropertyName("confidence_label")]
    public string ConfidenceLabel { get; set; } = "Low";

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];
}

public class BulkProductPredictionRequestDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("sales_history")]
    public List<SalesHistoryPointDto> SalesHistory { get; set; } = [];

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

public class BulkPredictionRequestDto
{
    [JsonPropertyName("products")]
    public List<BulkProductPredictionRequestDto> Products { get; set; } = [];
}

public class BulkProductPredictionResponseDto
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("predicted_next_day_sales")]
    public decimal PredictedNextDaySales { get; set; }

    [JsonPropertyName("daily_forecasts")]
    public List<DailyForecastPointDto> DailyForecasts { get; set; } = [];

    [JsonPropertyName("predicted_next_7_days_total")]
    public decimal PredictedNext7DaysTotal { get; set; }

    [JsonPropertyName("input_history_days")]
    public int InputHistoryDays { get; set; }

    [JsonPropertyName("confidence_label")]
    public string ConfidenceLabel { get; set; } = "Low";

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];
}

public class BulkPredictionResponseDto
{
    [JsonPropertyName("results")]
    public List<BulkProductPredictionResponseDto> Results { get; set; } = [];
}
