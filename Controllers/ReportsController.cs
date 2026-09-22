using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Report types: BirthTrends, GenderDistribution, CountryComparison,
        // CityLevel, HistoricalData, PredictionResults, HistoricalVsPredicted
        public async Task<IActionResult> Index(string reportType = "BirthTrends", int? countryId = null, int? yearFrom = null, int? yearTo = null)
        {
            var vm = new ReportsViewModel
            {
                Countries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(),
                ReportType = reportType,
                CountryId = countryId,
                YearFrom = yearFrom,
                YearTo = yearTo
            };

            var historyQuery = _db.BirthRecords.AsNoTracking().Include(r => r.Country).Include(r => r.City).AsQueryable();
            var predictionQuery = _db.Predictions.AsNoTracking().Include(p => p.Country).Include(p => p.City).AsQueryable();

            if (countryId.HasValue)
            {
                historyQuery = historyQuery.Where(r => r.CountryId == countryId);
                predictionQuery = predictionQuery.Where(p => p.CountryId == countryId);
            }
            if (yearFrom.HasValue)
            {
                historyQuery = historyQuery.Where(r => r.Year >= yearFrom);
                predictionQuery = predictionQuery.Where(p => p.Year >= yearFrom);
            }
            if (yearTo.HasValue)
            {
                historyQuery = historyQuery.Where(r => r.Year <= yearTo);
                predictionQuery = predictionQuery.Where(p => p.Year <= yearTo);
            }

            vm.HistoricalRows = await historyQuery.OrderBy(r => r.Country!.CountryName).ThenBy(r => r.Year).ToListAsync();
            vm.PredictionRows = await predictionQuery.OrderBy(p => p.Country!.CountryName).ThenBy(p => p.Year).ToListAsync();

            vm.TotalBirthsSum = vm.HistoricalRows.Sum(r => (long)r.TotalBirths);
            vm.TotalMaleSum = vm.HistoricalRows.Sum(r => (long)r.MaleBirths);
            vm.TotalFemaleSum = vm.HistoricalRows.Sum(r => (long)r.FemaleBirths);

            return View(vm);
        }
    }
}
