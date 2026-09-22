using HumanBirthPredictionSystem.ViewModels;

namespace HumanBirthPredictionSystem.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> BuildDashboardAsync();
    }
}
