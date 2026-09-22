using HumanBirthPredictionSystem.Services;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class BirthTrendsController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public BirthTrendsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(int[]? countryIds, int? cityId, int? yearFrom, int? yearTo)
        {
            var filters = new BirthTrendsViewModel
            {
                SelectedCountryIds = countryIds?.ToList() ?? new List<int>(),
                CityId = cityId,
                YearFrom = yearFrom,
                YearTo = yearTo
            };

            var vm = await _analyticsService.GetBirthTrendsAsync(filters);
            return View(vm);
        }
    }
}
