using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModernPosSystem.Configurations;
using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Services;

public class ForecastApiClient(
    HttpClient httpClient,
    ILogger<ForecastApiClient> logger) : IForecastApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ServiceResult<ForecastApiHealthDto>> GetHealthAsync() =>
        SendAsync<ForecastApiHealthDto>(new HttpRequestMessage(HttpMethod.Get, "/health"));

    public Task<ServiceResult<NextDayPredictionResponseDto>> PredictNextDayAsync(NextDayPredictionRequestDto request) =>
        SendAsync<NextDayPredictionResponseDto>(BuildPostRequest("/predict/next-day", request));

    public Task<ServiceResult<Next7DaysPredictionResponseDto>> PredictNext7DaysAsync(Next7DaysPredictionRequestDto request) =>
        SendAsync<Next7DaysPredictionResponseDto>(BuildPostRequest("/predict/next-7-days", request));

    public Task<ServiceResult<BulkPredictionResponseDto>> PredictBulkAsync(BulkPredictionRequestDto request) =>
        SendAsync<BulkPredictionResponseDto>(BuildPostRequest("/predict/bulk", request));

    private static HttpRequestMessage BuildPostRequest<T>(string url, T payload) =>
        new(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

    private async Task<ServiceResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Forecast API request to {Path} failed with status {StatusCode}: {Body}", request.RequestUri, response.StatusCode, body);
                return ServiceResult<T>.Failure($"Forecast service request failed ({(int)response.StatusCode}).");
            }

            var payload = JsonSerializer.Deserialize<T>(body, JsonOptions);
            return payload is null
                ? ServiceResult<T>.Failure("Forecast service returned an empty response.")
                : ServiceResult<T>.Success(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Forecast API request to {Path} failed.", request.RequestUri);
            return ServiceResult<T>.Failure("Forecast service is currently unavailable.");
        }
    }
}

public class SalesHistoryAggregationService(
    IRepository<Sale> saleRepository) : ISalesHistoryAggregationService
{
    public async Task<List<DailySalesHistoryDto>> GetDailySalesHistoryAsync(string storeId, string productId, int lookbackDays, DateTime forecastDate)
    {
        var startDate = forecastDate.Date.AddDays(-lookbackDays);
        var endDate = forecastDate.Date.AddDays(-1);
        var sales = await saleRepository.GetAllAsync(x => x.IsActive && x.SaleDate >= startDate && x.SaleDate < forecastDate.Date.AddDays(1));

        var quantitiesByDate = sales
            .SelectMany(sale => sale.Items
                .Where(item => item.ProductId == productId)
                .Select(item => new { Date = sale.SaleDate.Date, item.Quantity }))
            .GroupBy(x => x.Date)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));

        return Enumerable.Range(0, lookbackDays)
            .Select(offset => startDate.AddDays(offset))
            .Where(date => date <= endDate)
            .Select(date => new DailySalesHistoryDto
            {
                Date = date,
                Quantity = quantitiesByDate.TryGetValue(date, out var quantity) ? quantity : 0
            })
            .ToList();
    }

    public async Task<Dictionary<string, List<DailySalesHistoryDto>>> GetDailySalesHistoriesForActiveProductsAsync(string storeId, int lookbackDays, DateTime forecastDate)
    {
        var startDate = forecastDate.Date.AddDays(-lookbackDays);
        var sales = await saleRepository.GetAllAsync(x => x.IsActive && x.SaleDate >= startDate && x.SaleDate < forecastDate.Date.AddDays(1));
        var itemLookup = sales
            .SelectMany(sale => sale.Items.Select(item => new { sale.SaleDate, Item = item }))
            .GroupBy(x => x.Item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var result = new Dictionary<string, List<DailySalesHistoryDto>>();
        foreach (var entry in itemLookup)
        {
            var quantitiesByDate = entry.Value
                .GroupBy(x => x.SaleDate.Date)
                .ToDictionary(group => group.Key, group => group.Sum(row => row.Item.Quantity));

            result[entry.Key] = Enumerable.Range(0, lookbackDays)
                .Select(offset => startDate.AddDays(offset))
                .Select(date => new DailySalesHistoryDto
                {
                    Date = date,
                    Quantity = quantitiesByDate.TryGetValue(date, out var quantity) ? quantity : 0
                })
                .ToList();
        }

        return result;
    }
}

public class ForecastGenerationService(
    IRepository<Product> productRepository,
    IRepository<ForecastDocument> forecastRepository,
    IRepository<AppSetting> settingRepository,
    IForecastApiClient forecastApiClient,
    ISalesHistoryAggregationService salesHistoryAggregationService,
    IOptions<ForecastingOptions> options,
    ILogger<ForecastGenerationService> logger) : IForecastGenerationService
{
    private readonly ForecastingOptions _options = options.Value;

    public Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForProductAsync(ForecastGenerationRequestDto request, string userName) =>
        GenerateInternalAsync(request, userName, singleProductOnly: true);

    public Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForStoreAsync(ForecastGenerationRequestDto request, string userName) =>
        GenerateInternalAsync(request, userName, singleProductOnly: false);

    public Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForAllActiveProductsAsync(ForecastGenerationRequestDto request, string userName) =>
        GenerateInternalAsync(request, userName, singleProductOnly: false);

    private async Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateInternalAsync(ForecastGenerationRequestDto request, string userName, bool singleProductOnly)
    {
        var forecastDate = request.ForecastDate == default ? DateTime.UtcNow.Date : request.ForecastDate.Date;
        var storeId = string.IsNullOrWhiteSpace(request.StoreId) ? _options.DefaultStoreId : request.StoreId.Trim();
        var generationSource = string.IsNullOrWhiteSpace(request.GenerationSource) ? ForecastGenerationSources.Manual : request.GenerationSource.Trim();
        var products = await productRepository.GetAllAsync(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(request.CategoryId))
        {
            products = products.Where(x => x.CategoryId == request.CategoryId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.ProductId))
        {
            products = products.Where(x => x.Id == request.ProductId).ToList();
        }

        if (singleProductOnly && string.IsNullOrWhiteSpace(request.ProductId))
        {
            return ServiceResult<ForecastGenerationSummaryDto>.Failure("Select a product before generating a single-product forecast.");
        }

        if (products.Count == 0)
        {
            return ServiceResult<ForecastGenerationSummaryDto>.Failure("No active products matched the selected forecast filters.");
        }

        var warnings = new List<string>();
        var bulkRequest = new BulkPredictionRequestDto();
        var requestMap = new Dictionary<int, ForecastRequestDto>();

        foreach (var product in products)
        {
            if (product.ForecastItemCode <= 0)
            {
                warnings.Add($"{product.ProductName} was skipped because it does not have a forecast item code.");
                continue;
            }

            var history = await salesHistoryAggregationService.GetDailySalesHistoryAsync(storeId, product.Id, _options.MinimumHistoryDays, forecastDate);
            if (history.Count == 0)
            {
                warnings.Add($"{product.ProductName} was skipped because no sales history could be aggregated.");
                continue;
            }

            requestMap[product.ForecastItemCode] = new ForecastRequestDto
            {
                StoreId = storeId,
                ForecastStoreCode = _options.DefaultForecastStoreCode,
                ProductId = product.Id,
                ProductName = product.ProductName,
                ProductCode = product.ProductCode,
                ForecastItemCode = product.ForecastItemCode,
                CategoryId = product.CategoryId,
                CategoryName = product.CategoryName,
                ForecastDate = forecastDate,
                SalesHistory = history,
                Debug = request.Debug
            };

            bulkRequest.Products.Add(new BulkProductPredictionRequestDto
            {
                StoreId = _options.DefaultForecastStoreCode,
                ItemId = product.ForecastItemCode,
                StartDate = forecastDate.ToString("yyyy-MM-dd"),
                SalesHistory = history.Select(point => new SalesHistoryPointDto
                {
                    Date = point.Date.ToString("yyyy-MM-dd"),
                    Quantity = point.Quantity
                }).ToList(),
                Debug = request.Debug
            });
        }

        if (bulkRequest.Products.Count == 0)
        {
            return ServiceResult<ForecastGenerationSummaryDto>.Failure(string.Join(" ", warnings.DefaultIfEmpty("No eligible products were available for forecasting.")));
        }

        var apiResponse = await forecastApiClient.PredictBulkAsync(bulkRequest);
        if (!apiResponse.Succeeded || apiResponse.Data is null)
        {
            return ServiceResult<ForecastGenerationSummaryDto>.Failure(apiResponse.Message);
        }

        var now = DateTime.UtcNow;
        var generatedCount = 0;
        var responseLookup = apiResponse.Data.Results.ToDictionary(x => x.ItemId, x => x);

        foreach (var itemCode in requestMap.Keys)
        {
            var sourceRequest = requestMap[itemCode];
            if (!responseLookup.TryGetValue(itemCode, out var prediction))
            {
                warnings.Add($"{sourceRequest.ProductName} did not receive a prediction result from the forecasting service.");
                continue;
            }

            var averageRecentSales = sourceRequest.SalesHistory.Any()
                ? sourceRequest.SalesHistory.Average(x => x.Quantity)
                : 0;
            var product = products.First(x => x.Id == sourceRequest.ProductId);
            var safetyStock = product.SafetyStock ?? Math.Max(product.ReorderLevel, _options.DefaultSafetyStock);
            var reorderStatus = ResolveReorderStatus(product.CurrentStock, safetyStock, prediction.PredictedNext7DaysTotal);
            var reorderQty = Math.Max(0, Math.Ceiling(prediction.PredictedNext7DaysTotal + safetyStock - product.CurrentStock));
            var isSlowMoving = prediction.PredictedNext7DaysTotal < _options.SlowMovingMaxNext7Days &&
                (product.CurrentStock > _options.SlowMovingHighStockThreshold || averageRecentSales < 1);

            var forecastDateEnd = forecastDate.AddDays(1);
            var forecastDocument = await forecastRepository.GetFirstOrDefaultAsync(x =>
                x.IsActive &&
                x.StoreId == storeId &&
                x.ProductId == sourceRequest.ProductId &&
                x.ForecastStartDate >= forecastDate &&
                x.ForecastStartDate < forecastDateEnd);

            if (forecastDocument is null)
            {
                forecastDocument = new ForecastDocument
                {
                    StoreId = storeId,
                    ForecastStoreCode = _options.DefaultForecastStoreCode,
                    ProductId = sourceRequest.ProductId,
                    ForecastItemCode = sourceRequest.ForecastItemCode,
                    ProductName = sourceRequest.ProductName,
                    ProductCode = sourceRequest.ProductCode,
                    CategoryId = sourceRequest.CategoryId,
                    CategoryName = sourceRequest.CategoryName,
                    CreatedBy = userName
                };
                await ApplyForecastAsync(forecastDocument, forecastDate, generationSource, prediction, product, averageRecentSales, safetyStock, reorderQty, reorderStatus, isSlowMoving, now, userName);
                await forecastRepository.AddAsync(forecastDocument);
            }
            else
            {
                await ApplyForecastAsync(forecastDocument, forecastDate, generationSource, prediction, product, averageRecentSales, safetyStock, reorderQty, reorderStatus, isSlowMoving, now, userName);
                await forecastRepository.UpdateAsync(forecastDocument);
            }

            generatedCount++;
            if (prediction.Warnings.Count > 0)
            {
                warnings.AddRange(prediction.Warnings.Select(warning => $"{sourceRequest.ProductName}: {warning}"));
            }
        }

        if (generatedCount > 0)
        {
            await UpsertSettingAsync(
                generationSource == ForecastGenerationSources.Auto ? AppSettingKeys.ForecastLastAutoGeneratedAtUtc : AppSettingKeys.ForecastLastManualGeneratedAtUtc,
                now.ToString("O"),
                generationSource == ForecastGenerationSources.Auto ? "Last successful automatic forecast run timestamp (UTC)" : "Last successful manual forecast run timestamp (UTC)",
                userName);

            if (generationSource == ForecastGenerationSources.Auto)
            {
                await UpsertSettingAsync(
                    AppSettingKeys.ForecastLastAutoGeneratedDateLocal,
                    DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"),
                    "Last successful automatic forecast local date marker",
                    userName);
            }
        }

        logger.LogInformation("Generated {Count} {Source} forecast documents for store {StoreId} and forecast date {ForecastDate}.", generatedCount, generationSource, storeId, forecastDate);

        return ServiceResult<ForecastGenerationSummaryDto>.Success(new ForecastGenerationSummaryDto
        {
            RequestedProducts = products.Count,
            GeneratedProducts = generatedCount,
            SkippedProducts = Math.Max(0, products.Count - generatedCount),
            Warnings = warnings
        }, generatedCount > 0
            ? $"Forecast generated for {generatedCount} product(s)."
            : "No forecast records were generated.");
    }

    private Task ApplyForecastAsync(
        ForecastDocument document,
        DateTime forecastDate,
        string generationSource,
        BulkProductPredictionResponseDto prediction,
        Product product,
        decimal averageRecentSales,
        decimal safetyStock,
        decimal reorderQty,
        ReorderStatus reorderStatus,
        bool isSlowMoving,
        DateTime generatedAt,
        string userName)
    {
        document.ForecastGeneratedAt = generatedAt;
        document.ForecastStartDate = forecastDate.Date;
        document.GeneratedSource = generationSource;
        document.PredictedNextDaySales = prediction.PredictedNextDaySales;
        document.PredictedNext7DaysTotal = prediction.PredictedNext7DaysTotal;
        document.DailyForecasts = prediction.DailyForecasts.Select(item => new ForecastDailyItem
        {
            Date = DateTime.Parse(item.Date).Date,
            PredictedQuantity = item.PredictedQuantity
        }).ToList();
        document.CurrentStock = product.CurrentStock;
        document.SafetyStock = safetyStock;
        document.ReorderSuggestedQuantity = reorderQty;
        document.ReorderStatus = reorderStatus;
        document.IsSlowMoving = isSlowMoving;
        document.AverageRecentDailySales = Math.Round(averageRecentSales, 2);
        document.ForecastModelName = _options.ModelName;
        document.ForecastVersion = _options.ModelVersion;
        document.ConfidenceLabel = prediction.ConfidenceLabel;
        document.InputHistoryDays = prediction.InputHistoryDays;
        document.Warnings = prediction.Warnings;
        document.ReorderExplanation = BuildReorderExplanation(product.CurrentStock, prediction.PredictedNext7DaysTotal, safetyStock, reorderQty, reorderStatus);
        document.SlowMovingExplanation = BuildSlowMovingExplanation(isSlowMoving, product.CurrentStock, prediction.PredictedNext7DaysTotal, averageRecentSales);
        document.UpdatedAt = generatedAt;
        document.UpdatedBy = userName;
        document.CreatedAt = document.CreatedAt == default ? generatedAt : document.CreatedAt;
        return Task.CompletedTask;
    }

    private async Task UpsertSettingAsync(string key, string value, string description, string userName)
    {
        var existing = await settingRepository.GetFirstOrDefaultAsync(x => x.Key == key);
        if (existing is null)
        {
            await settingRepository.AddAsync(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedBy = userName
            });
            return;
        }

        existing.Value = value;
        existing.Description = description;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await settingRepository.UpdateAsync(existing);
    }

    private static ReorderStatus ResolveReorderStatus(decimal currentStock, decimal safetyStock, decimal predictedNext7DaysTotal)
    {
        if (currentStock <= Math.Max(1, safetyStock / 2) && predictedNext7DaysTotal > currentStock)
        {
            return ReorderStatus.Critical;
        }

        return predictedNext7DaysTotal + safetyStock > currentStock
            ? ReorderStatus.ReorderNow
            : ReorderStatus.Healthy;
    }

    private static string BuildReorderExplanation(decimal currentStock, decimal predictedNext7DaysTotal, decimal safetyStock, decimal reorderQty, ReorderStatus reorderStatus)
    {
        var requiredStock = predictedNext7DaysTotal + safetyStock;
        return reorderStatus switch
        {
            ReorderStatus.Critical => $"Current stock of {currentStock:N0} is critically low against required stock {requiredStock:N0}. Suggested reorder quantity is {reorderQty:N0}.",
            ReorderStatus.ReorderNow => $"Predicted 7-day demand plus safety stock requires {requiredStock:N0} units. Current stock is {currentStock:N0}, so reorder {reorderQty:N0}.",
            _ => $"Current stock of {currentStock:N0} comfortably covers the predicted 7-day demand plus safety stock requirement of {requiredStock:N0}."
        };
    }

    private static string BuildSlowMovingExplanation(bool isSlowMoving, decimal currentStock, decimal predictedNext7DaysTotal, decimal averageRecentSales) =>
        isSlowMoving
            ? $"The product is flagged as slow-moving because predicted 7-day demand is {predictedNext7DaysTotal:N2} while current stock is {currentStock:N0} and average recent daily sales are {averageRecentSales:N2}."
            : $"Demand and stock balance do not currently match the slow-moving threshold. Predicted 7-day demand is {predictedNext7DaysTotal:N2}.";
}

public class ForecastProviderService(
    IRepository<ForecastDocument> forecastRepository,
    IRepository<Product> productRepository,
    IRepository<Category> categoryRepository,
    IRepository<AppSetting> settingRepository,
    IForecastApiClient forecastApiClient,
    IOptions<ForecastingOptions> options) : IForecastProviderService
{
    private readonly ForecastingOptions _options = options.Value;

    public async Task<ForecastingPageViewModel> GetForecastingAsync(ForecastQueryDto filter)
    {
        var storeId = string.IsNullOrWhiteSpace(filter.StoreId) ? _options.DefaultStoreId : filter.StoreId.Trim();
        var forecastDate = filter.ForecastDate == default ? DateTime.UtcNow.Date : filter.ForecastDate.Date;
        var forecastDateEnd = forecastDate.AddDays(1);
        var filtered = await forecastRepository.GetAllAsync(x =>
            x.IsActive &&
            x.StoreId == storeId &&
            x.ForecastStartDate >= forecastDate &&
            x.ForecastStartDate < forecastDateEnd);
        var products = await productRepository.GetAllAsync(x => x.IsActive);
        var categories = await categoryRepository.GetAllAsync(x => x.IsActive);
        var settings = await settingRepository.GetAllAsync(x => x.IsActive);
        var health = await forecastApiClient.GetHealthAsync();

        if (!string.IsNullOrWhiteSpace(filter.ProductId))
        {
            filtered = filtered.Where(x => x.ProductId == filter.ProductId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.CategoryId))
        {
            filtered = filtered.Where(x => x.CategoryId == filter.CategoryId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.ReorderStatus) && Enum.TryParse<ReorderStatus>(filter.ReorderStatus.Replace(" ", string.Empty), true, out var reorderStatus))
        {
            filtered = filtered.Where(x => x.ReorderStatus == reorderStatus).ToList();
        }

        if (filter.SlowMovingOnly)
        {
            filtered = filtered.Where(x => x.IsSlowMoving).ToList();
        }

        var topDemand = filtered.OrderByDescending(x => x.PredictedNext7DaysTotal).Take(8).ToList();
        var reorderNeeded = filtered.Where(x => x.ReorderStatus != ReorderStatus.Healthy).OrderByDescending(x => x.ReorderSuggestedQuantity).Take(8).ToList();
        var slowMoving = filtered.Where(x => x.IsSlowMoving).OrderByDescending(x => x.CurrentStock).Take(8).ToList();

        return new ForecastingPageViewModel
        {
            Filter = new ForecastFilterViewModel
            {
                StoreId = storeId,
                ForecastDate = forecastDate,
                ProductId = filter.ProductId,
                CategoryId = filter.CategoryId,
                ReorderStatus = filter.ReorderStatus,
                SlowMovingOnly = filter.SlowMovingOnly
            },
            StoreOptions =
            [
                new LookupOptionViewModel { Id = _options.DefaultStoreId, Name = "Main Store" }
            ],
            CategoryOptions = categories
                .OrderBy(x => x.CategoryName)
                .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.CategoryName })
                .ToList(),
            ProductOptions = products
                .OrderBy(x => x.ProductName)
                .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.ProductName })
                .ToList(),
            Cards =
            [
                new() { Title = "Products Forecasted", Value = filtered.Count.ToString(), Subtitle = "Forecast documents for the selected date", Icon = "bi bi-box-seam", AccentClass = "accent-blue" },
                new() { Title = "Need Reorder", Value = filtered.Count(x => x.ReorderStatus != ReorderStatus.Healthy).ToString(), Subtitle = "Products requiring replenishment", Icon = "bi bi-arrow-repeat", AccentClass = "accent-amber" },
                new() { Title = "Slow Moving", Value = filtered.Count(x => x.IsSlowMoving).ToString(), Subtitle = "Low-demand stock to monitor", Icon = "bi bi-graph-down-arrow", AccentClass = "accent-violet" },
                new() { Title = "Next-Day Demand", Value = filtered.Sum(x => x.PredictedNextDaySales).ToString("N1"), Subtitle = "Predicted units for tomorrow", Icon = "bi bi-sunrise", AccentClass = "accent-emerald" },
                new() { Title = "Next-7-Day Demand", Value = filtered.Sum(x => x.PredictedNext7DaysTotal).ToString("N1"), Subtitle = "Predicted units for the coming week", Icon = "bi bi-calendar-week", AccentClass = "accent-slate" }
            ],
            Results = filtered
                .OrderByDescending(x => x.ReorderStatus)
                .ThenByDescending(x => x.PredictedNext7DaysTotal)
                .Select(x => new ForecastResultRowViewModel
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    ProductCode = x.ProductCode,
                    CategoryName = x.CategoryName,
                    CurrentStock = x.CurrentStock,
                    PredictedNextDaySales = x.PredictedNextDaySales,
                    PredictedNext7DaysTotal = x.PredictedNext7DaysTotal,
                    SafetyStock = x.SafetyStock,
                    ReorderSuggestedQuantity = x.ReorderSuggestedQuantity,
                    ReorderStatus = FormatReorderStatus(x.ReorderStatus),
                    IsSlowMoving = x.IsSlowMoving,
                    ConfidenceLabel = x.ConfidenceLabel,
                    ForecastGeneratedAt = x.ForecastGeneratedAt,
                    Warnings = x.Warnings
                }).ToList(),
            TopDemandChart = topDemand.Select(x => new ChartSeriesViewModel { Label = x.ProductName, Value = x.PredictedNext7DaysTotal }).ToList(),
            ReorderChart = reorderNeeded.Select(x => new ChartSeriesViewModel { Label = x.ProductName, Value = x.ReorderSuggestedQuantity }).ToList(),
            SlowMovingChart = slowMoving.Select(x => new ChartSeriesViewModel { Label = x.ProductName, Value = x.CurrentStock }).ToList(),
            CategoryDemandChart = filtered
                .GroupBy(x => x.CategoryName)
                .Select(group => new ChartSeriesViewModel
                {
                    Label = group.Key,
                    Value = group.Sum(item => item.PredictedNext7DaysTotal)
                })
                .OrderByDescending(x => x.Value)
                .ToList(),
            ServiceStatus = health.Succeeded && health.Data is not null
                ? $"{health.Data.Status} / model {(health.Data.ModelLoaded ? "ready" : "missing")}"
                : health.Message,
            ServiceHealthy = health.Succeeded && health.Data is not null && health.Data.ModelLoaded && health.Data.FeatureColumnsLoaded,
            LastGeneratedAt = filtered.OrderByDescending(x => x.ForecastGeneratedAt).Select(x => (DateTime?)x.ForecastGeneratedAt).FirstOrDefault(),
            LastAutoGeneratedAt = ParseUtcSetting(settings, AppSettingKeys.ForecastLastAutoGeneratedAtUtc),
            LastManualGeneratedAt = ParseUtcSetting(settings, AppSettingKeys.ForecastLastManualGeneratedAtUtc),
            NextScheduledRunAt = _options.AutoRunEnabled ? ResolveNextScheduledRun(DateTime.Now, _options.AutoRunLocalTime) : null,
            AutoRunEnabled = _options.AutoRunEnabled
        };
    }

    public async Task<ForecastDetailViewModel?> GetForecastDetailsAsync(string id)
    {
        var forecast = await forecastRepository.GetByIdAsync(id);
        if (forecast is null)
        {
            return null;
        }

        return new ForecastDetailViewModel
        {
            Id = forecast.Id,
            ProductName = forecast.ProductName,
            ProductCode = forecast.ProductCode,
            CurrentStock = forecast.CurrentStock,
            AverageRecentDailySales = forecast.AverageRecentDailySales,
            PredictedNextDaySales = forecast.PredictedNextDaySales,
            PredictedNext7DaysTotal = forecast.PredictedNext7DaysTotal,
            SafetyStock = forecast.SafetyStock,
            ReorderSuggestedQuantity = forecast.ReorderSuggestedQuantity,
            ReorderStatus = FormatReorderStatus(forecast.ReorderStatus),
            IsSlowMoving = forecast.IsSlowMoving,
            SlowMovingExplanation = forecast.SlowMovingExplanation,
            ReorderExplanation = forecast.ReorderExplanation,
            ConfidenceLabel = forecast.ConfidenceLabel,
            InputHistoryDays = forecast.InputHistoryDays,
            ForecastModelName = forecast.ForecastModelName,
            ForecastVersion = forecast.ForecastVersion,
            ForecastGeneratedAt = forecast.ForecastGeneratedAt,
            DailyForecasts = forecast.DailyForecasts
                .OrderBy(x => x.Date)
                .Select(x => new ForecastDetailDailyItemViewModel
                {
                    Date = x.Date,
                    PredictedQuantity = x.PredictedQuantity
                }).ToList(),
            Warnings = forecast.Warnings
        };
    }

    private static string FormatReorderStatus(ReorderStatus status) => status switch
    {
        ReorderStatus.ReorderNow => "Reorder Now",
        ReorderStatus.Critical => "Critical",
        _ => "Healthy"
    };

    private static DateTime? ParseUtcSetting(IEnumerable<AppSetting> settings, string key)
    {
        var value = settings.FirstOrDefault(x => x.Key == key)?.Value;
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    private static DateTime? ResolveNextScheduledRun(DateTime nowLocal, string configuredTime)
    {
        if (!TimeSpan.TryParse(configuredTime, out var scheduledLocalTime))
        {
            return null;
        }

        var candidate = nowLocal.Date.Add(scheduledLocalTime);
        return candidate > nowLocal ? candidate : candidate.AddDays(1);
    }
}
