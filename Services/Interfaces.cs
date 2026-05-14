using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Services;

public interface IAuthService
{
    Task<ServiceResult<SystemUser>> AuthenticateAsync(string username, string password);
    Task SignInAsync(HttpContext context, SystemUser user, bool rememberMe);
    Task SignOutAsync(HttpContext context);
}

public interface IPermissionService
{
    Task<bool> HasAccessAsync(string roleId, string formKey);
    Task<List<SidebarMenuItemViewModel>> GetSidebarMenuAsync(string roleId, string currentPath);
    Task<List<LookupOptionViewModel>> GetRoleOptionsAsync();
    Task<List<RoleFormViewModel>> GetRolesAsync();
    Task<RoleFormViewModel> GetRoleFormAsync(string? id = null);
    Task<ServiceResult> SaveRoleAsync(RoleFormViewModel model, string userName);
    Task<ServiceResult> ToggleRoleStatusAsync(string id, string userName);
    Task<RolePermissionMatrixViewModel> GetPermissionMatrixAsync(string? roleId);
    Task<ServiceResult> SavePermissionMatrixAsync(RolePermissionMatrixViewModel model, string userName);
}

public interface IProductService
{
    Task<List<ProductListItemViewModel>> GetListAsync(string? searchTerm = null, string? categoryId = null);
    Task<ProductFormViewModel> GetFormAsync(string? id = null);
    Task<ProductFormViewModel> PrepareFormAsync(ProductFormViewModel model);
    Task<ServiceResult> SaveAsync(ProductFormViewModel model, string userName);
    Task<ServiceResult> DeleteAsync(string id, string userName);
    Task<List<LookupOptionViewModel>> GetLookupAsync();
    Task<List<PosProductCardViewModel>> SearchForPosAsync(string? query = null);
    Task<Product?> GetByIdAsync(string id);
}

public interface ICategoryService
{
    Task<List<CategoryListItemViewModel>> GetListAsync();
    Task<CategoryFormViewModel> GetFormAsync(string? id = null);
    Task<ServiceResult> SaveAsync(CategoryFormViewModel model, string userName);
    Task<ServiceResult> DeleteAsync(string id, string userName);
    Task<List<LookupOptionViewModel>> GetLookupAsync();
}

public interface ICustomerService
{
    Task<List<CustomerListItemViewModel>> GetListAsync(string? searchTerm = null);
    Task<CustomerFormViewModel> GetFormAsync(string? id = null);
    Task<ServiceResult> SaveAsync(CustomerFormViewModel model, string userName);
    Task<ServiceResult> DeleteAsync(string id, string userName);
    Task<List<LookupOptionViewModel>> GetLookupAsync();
    Task<List<PosCustomerOptionViewModel>> GetPosLookupAsync();
    Task<Customer?> GetByIdAsync(string id);
    Task<string> GetWalkInCustomerIdAsync();
    Task<List<SalesHistoryItemViewModel>> GetPurchaseHistoryAsync(string customerId);
}

public interface ISupplierService
{
    Task<List<SupplierListItemViewModel>> GetListAsync(string? searchTerm = null);
    Task<SupplierFormViewModel> GetFormAsync(string? id = null);
    Task<ServiceResult> SaveAsync(SupplierFormViewModel model, string userName);
    Task<ServiceResult> DeleteAsync(string id, string userName);
    Task<List<LookupOptionViewModel>> GetLookupAsync();
}

public interface IUserService
{
    Task<List<UserListItemViewModel>> GetListAsync();
    Task<UserFormViewModel> GetFormAsync(string? id = null);
    Task<ServiceResult> SaveAsync(UserFormViewModel model, string userName);
    Task<ServiceResult> ToggleStatusAsync(string id, string userName);
    Task<ServiceResult> ResetPasswordAsync(string id, string newPassword, string userName);
    Task<List<LookupOptionViewModel>> GetCashierLookupAsync();
}

public interface IInventoryService
{
    Task<InventoryOverviewViewModel> GetOverviewAsync();
    Task<StockAdjustmentViewModel> GetAdjustmentFormAsync();
    Task<ServiceResult> SaveAdjustmentAsync(StockAdjustmentViewModel model, ICurrentUserContext currentUser);
    Task RecordMovementAsync(InventoryMovement movement);
}

public interface IPurchaseService
{
    Task<PurchasePageViewModel> GetPageAsync();
    Task<ServiceResult> SaveAsync(PurchaseFormViewModel model, ICurrentUserContext currentUser);
    Task<PurchaseDetailViewModel?> GetDetailsAsync(string id);
}

public interface ISalesService
{
    Task<string> GetNextInvoiceNumberAsync();
    Task<string> GetNextGrnNumberAsync();
    Task<SalesHistoryPageViewModel> GetHistoryAsync(string? invoiceNo, DateTime? fromDate, DateTime? toDate, string? cashierUserId);
    Task<InvoiceViewModel?> GetInvoiceAsync(string id);
    Task<List<Sale>> GetSalesAsync(DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IPosService
{
    Task<PosPageViewModel> GetPageAsync(ICurrentUserContext currentUser);
    Task<ServiceResult<string>> CompleteCheckoutAsync(CheckoutRequestDto model, ICurrentUserContext currentUser);
}

public interface ILoyaltyPolicyService
{
    Task<LoyaltyPolicyViewModel> GetPolicyAsync();
}

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();
}

public interface IReportService
{
    Task<ReportsPageViewModel> GetReportsAsync(ReportFilterDto filter);
    Task<FileExportResult> ExportExcelAsync(ReportFilterDto filter);
    Task<FileExportResult> ExportPdfAsync(ReportFilterDto filter);
}

public interface IForecastProviderService
{
    Task<ForecastingPageViewModel> GetForecastingAsync(ForecastQueryDto filter);
    Task<ForecastDetailViewModel?> GetForecastDetailsAsync(string id);
}

public interface IForecastApiClient
{
    Task<ServiceResult<ForecastApiHealthDto>> GetHealthAsync();
    Task<ServiceResult<NextDayPredictionResponseDto>> PredictNextDayAsync(NextDayPredictionRequestDto request);
    Task<ServiceResult<Next7DaysPredictionResponseDto>> PredictNext7DaysAsync(Next7DaysPredictionRequestDto request);
    Task<ServiceResult<BulkPredictionResponseDto>> PredictBulkAsync(BulkPredictionRequestDto request);
}

public interface ISalesHistoryAggregationService
{
    Task<List<DailySalesHistoryDto>> GetDailySalesHistoryAsync(string storeId, string productId, int lookbackDays, DateTime forecastDate);
    Task<Dictionary<string, List<DailySalesHistoryDto>>> GetDailySalesHistoriesForActiveProductsAsync(string storeId, int lookbackDays, DateTime forecastDate);
}

public interface IForecastGenerationService
{
    Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForProductAsync(ForecastGenerationRequestDto request, string userName);
    Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForStoreAsync(ForecastGenerationRequestDto request, string userName);
    Task<ServiceResult<ForecastGenerationSummaryDto>> GenerateForecastForAllActiveProductsAsync(ForecastGenerationRequestDto request, string userName);
}

public interface ISettingsService
{
    Task<List<AppSetting>> GetSettingsAsync();
}

public interface IDataSeeder
{
    Task SeedAsync();
}

public interface IImageStorageService
{
    Task<ServiceResult<string>> UploadProductImageAsync(IFormFile file, CancellationToken cancellationToken = default);
}
