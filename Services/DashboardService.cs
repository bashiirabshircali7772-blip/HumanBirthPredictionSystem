using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Services
{
    /// <summary>
    /// Aggregates data directly from SQL Server for the main dashboard:
    /// summary cards, trend charts, gender split, country comparison,
    /// and historical-vs-predicted overlay.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardViewModel> BuildDashboardAsync()
        {
            var vm = new DashboardViewModel
            {
                TotalCountries = await _db.Countries.CountAsync(),
                TotalCities = await _db.Cities.CountAsync(),
                TotalBirthRecords = await _db.BirthRecords.CountAsync()
            };

            var records = await _db.BirthRecords.AsNoTracking().ToListAsync();

            if (records.Count > 0)
            {
                vm.TotalMaleBirths = records.Sum(r => (long)r.MaleBirths);
                vm.TotalFemaleBirths = records.Sum(r => (long)r.FemaleBirths);
                var totalAll = vm.TotalMaleBirths + vm.TotalFemaleBirths;
                vm.MalePercentage = totalAll == 0 ? 0 : Math.Round((double)vm.TotalMaleBirths / totalAll * 100, 2);
                vm.FemalePercentage = totalAll == 0 ? 0 : Math.Round((double)vm.TotalFemaleBirths / totalAll * 100, 2);

                var latestYear = records.Max(r => r.Year);
                vm.LatestTotalBirths = records.Where(r => r.Year == latestYear).Sum(r => (long)r.TotalBirths);

                // Yearly trend (sum across all countries per year)
                var byYear = records
                    .GroupBy(r => r.Year)
                    .OrderBy(g => g.Key)
                    .ToList();

                vm.TrendYears = byYear.Select(g => g.Key).ToList();
                vm.TrendTotals = byYear.Select(g => g.Sum(r => (long)r.TotalBirths)).ToList();
                vm.TrendMale = byYear.Select(g => g.Sum(r => (long)r.MaleBirths)).ToList();
                vm.TrendFemale = byYear.Select(g => g.Sum(r => (long)r.FemaleBirths)).ToList();

                // Country comparison (latest year totals per country)
                var countryTotals = records
                    .Where(r => r.Year == latestYear)
                    .GroupBy(r => r.CountryId)
                    .Select(g => new { CountryId = g.Key, Total = g.Sum(r => (long)r.TotalBirths) })
                    .OrderByDescending(x => x.Total)
                    .Take(8)
                    .ToList();

                var countryLookup = await _db.Countries
                    .Where(c => countryTotals.Select(ct => ct.CountryId).Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.CountryName);

                vm.CountryNames = countryTotals.Select(ct => countryLookup.GetValueOrDefault(ct.CountryId, "Unknown")).ToList();
                vm.CountryTotals = countryTotals.Select(ct => ct.Total).ToList();

                // Recent records table (latest 8)
                vm.RecentRecords = records
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(8)
                    .Select(r => new RecentRecordRow
                    {
                        Country = countryLookup.GetValueOrDefault(r.CountryId, "Unknown"),
                        Year = r.Year,
                        TotalBirths = r.TotalBirths,
                        RecordType = r.RecordType.ToString()
                    }).ToList();
            }

            var predictions = await _db.Predictions
                .AsNoTracking()
                .Include(p => p.Country)
                .Include(p => p.City)
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            if (predictions.Count > 0)
            {
                var latestPredYear = predictions.Max(p => p.Year);
                vm.LatestPredictedBirths = predictions
                    .Where(p => p.Year == latestPredYear)
                    .Sum(p => (long)p.PredictedTotalBirths);

                vm.RecentPredictions = predictions.Take(8).Select(p => new RecentPredictionRow
                {
                    Country = p.Country?.CountryName ?? "Unknown",
                    City = p.City?.CityName,
                    Year = p.Year,
                    PredictedTotalBirths = p.PredictedTotalBirths,
                    PredictionModel = p.PredictionModel
                }).ToList();

                // Historical vs Predicted overlay, merged by year (aggregated across countries)
                var histByYear = records
                    .GroupBy(r => r.Year)
                    .ToDictionary(g => g.Key, g => g.Sum(r => (long)r.TotalBirths));

                var predByYear = predictions
                    .GroupBy(p => p.Year)
                    .ToDictionary(g => g.Key, g => g.Sum(p => (long)p.PredictedTotalBirths));

                var allYears = histByYear.Keys.Union(predByYear.Keys).OrderBy(y => y);
                vm.HistoricalVsPredicted = allYears.Select(y => new HistoricalVsPredictedPoint
                {
                    Year = y,
                    HistoricalTotal = histByYear.TryGetValue(y, out var h) ? h : null,
                    PredictedTotal = predByYear.TryGetValue(y, out var p) ? p : null
                }).ToList();
            }

            return vm;
        }
    }
}
