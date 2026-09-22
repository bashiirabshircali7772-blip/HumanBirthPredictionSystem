using HumanBirthPredictionSystem.ViewModels;

namespace HumanBirthPredictionSystem.Services
{
    public interface IAnalyticsService
    {
        Task<GenderDistributionViewModel> GetGenderDistributionAsync(GenderDistributionViewModel filters);
        Task<BirthTrendsViewModel> GetBirthTrendsAsync(BirthTrendsViewModel filters);
    }
}
