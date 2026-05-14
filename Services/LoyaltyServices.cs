using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;
using ModernPosSystem.Helpers;

namespace ModernPosSystem.Services;

public class LoyaltyPolicyService(IRepository<AppSetting> settingRepository) : ILoyaltyPolicyService
{
    public async Task<LoyaltyPolicyViewModel> GetPolicyAsync()
    {
        var settings = await settingRepository.GetAllAsync(x => x.IsActive);

        return new LoyaltyPolicyViewModel
        {
            EarnRateAmount = GetDecimal(settings, AppSettingKeys.LoyaltyEarnRate, 100),
            RedeemValue = GetDecimal(settings, AppSettingKeys.LoyaltyRedeemValue, 1),
            MinimumRedeemPoints = GetDecimal(settings, AppSettingKeys.LoyaltyMinimumRedeemPoints, 1)
        };
    }

    private static decimal GetDecimal(IEnumerable<AppSetting> settings, string key, decimal fallback)
    {
        var rawValue = settings.FirstOrDefault(x => x.Key == key)?.Value;
        return decimal.TryParse(rawValue, out var parsed) ? parsed : fallback;
    }
}
