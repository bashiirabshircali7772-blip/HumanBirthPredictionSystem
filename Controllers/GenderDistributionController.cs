using HumanBirthPredictionSystem.Services;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class GenderDistributionController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public GenderDistributionController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(int? countryId, int? cityId, int? year, int? yearFrom, int? yearTo)
        {
            var filters = new GenderDistributionViewModel
            {
                CountryId = countryId,
                CityId = cityId,
                Year = year,
                YearFrom = yearFrom,
                YearTo = yearTo
            };

            var vm = await _analyticsService.GetGenderDistributionAsync(filters);
            return View(vm);
        }
    }
}
