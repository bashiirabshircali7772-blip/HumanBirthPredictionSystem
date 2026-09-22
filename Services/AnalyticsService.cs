using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Services
{
    /// <summary>
    /// Handles Gender Distribution and Birth Trend analysis queries against
    /// SQL Server, applying the country / city / year filters selected by
    /// the administrator.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<GenderDistributionViewModel> GetGenderDistributionAsync(GenderDistributionViewModel filters)
        {
            filters.Countries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync();
            filters.Cities = filters.CountryId.HasValue
                ? await _db.Cities.Where(c => c.CountryId == filters.CountryId).OrderBy(c => c.CityName).ToListAsync()
                : new List<Models.City>();

            var query = _db.BirthRecords.AsNoTracking().AsQueryable();

            if (filters.CountryId.HasValue)
                query = query.Where(r => r.CountryId == filters.CountryId);
            if (filters.CityId.HasValue)
                query = query.Where(r => r.CityId == filters.CityId);
            if (filters.Year.HasValue)
                query = query.Where(r => r.Year == filters.Year);
            if (filters.YearFrom.HasValue)
                query = query.Where(r => r.Year >= filters.YearFrom);
            if (filters.YearTo.HasValue)
                query = query.Where(r => r.Year <= filters.YearTo);

            var records = await query.Include(r => r.Country).ToListAsync();

            filters.TotalMaleBirths = records.Sum(r => (long)r.MaleBirths);
            filters.TotalFemaleBirths = records.Sum(r => (long)r.FemaleBirths);
            var total = filters.TotalMaleBirths + filters.TotalFemaleBirths;
            filters.MalePercentage = total == 0 ? 0 : Math.Round((double)filters.TotalMaleBirths / total * 100, 2);
            filters.FemalePercentage = total == 0 ? 0 : Math.Round((double)filters.TotalFemaleBirths / total * 100, 2);

            filters.CountryComparison = records
                .GroupBy(r => r.Country!.CountryName)
                .Select(g =>
                {
                    var male = g.Sum(r => (long)r.MaleBirths);
                    var female = g.Sum(r => (long)r.FemaleBirths);
                    var t = male + female;
                    return new CountryGenderRow
                    {
                        CountryName = g.Key,
                        MaleBirths = male,
                        FemaleBirths = female,
                        MalePercentage = t == 0 ? 0 : Math.Round((double)male / t * 100, 2),
                        FemalePercentage = t == 0 ? 0 : Math.Round((double)female / t * 100, 2)
                    };
                })
                .OrderByDescending(r => r.MaleBirths + r.FemaleBirths)
                .Take(10)
                .ToList();

            return filters;
        }

        public async Task<BirthTrendsViewModel> GetBirthTrendsAsync(BirthTrendsViewModel filters)
        {
            filters.Countries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync();
            filters.Cities = filters.SelectedCountryIds.Count == 1
                ? await _db.Cities.Where(c => c.CountryId == filters.SelectedCountryIds[0]).OrderBy(c => c.CityName).ToListAsync()
                : new List<Models.City>();

            var query = _db.BirthRecords.AsNoTracking().Include(r => r.Country).AsQueryable();

            if (filters.SelectedCountryIds.Count > 0)
                query = query.Where(r => filters.SelectedCountryIds.Contains(r.CountryId));
            if (filters.CityId.HasValue)
                query = query.Where(r => r.CityId == filters.CityId);
            if (filters.YearFrom.HasValue)
                query = query.Where(r => r.Year >= filters.YearFrom);
            if (filters.YearTo.HasValue)
                query = query.Where(r => r.Year <= filters.YearTo);

            var records = await query.ToListAsync();

            var byYear = records.GroupBy(r => r.Year).OrderBy(g => g.Key).ToList();
            filters.Years = byYear.Select(g => g.Key).ToList();
            filters.TotalBirths = byYear.Select(g => g.Sum(r => (long)r.TotalBirths)).ToList();
            filters.MaleBirths = byYear.Select(g => g.Sum(r => (long)r.MaleBirths)).ToList();
            filters.FemaleBirths = byYear.Select(g => g.Sum(r => (long)r.FemaleBirths)).ToList();

            // Multi-country comparison series
            filters.CountrySeries = records
                .GroupBy(r => r.Country!.CountryName)
                .ToDictionary(
                    g => g.Key,
                    g => filters.Years.Select(y => g.Where(r => r.Year == y).Sum(r => (long)r.TotalBirths)).ToList());

            // Simple growth rate: first year total -> last year total
            if (filters.TotalBirths.Count >= 2 && filters.TotalBirths.First() > 0)
            {
                var first = filters.TotalBirths.First();
                var last = filters.TotalBirths.Last();
                filters.GrowthRatePercent = Math.Round((double)(last - first) / first * 100, 2);
            }

            return filters;
        }
    }
}
