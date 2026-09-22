using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Models;
using HumanBirthPredictionSystem.Services;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class PredictionController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPythonPredictionService _pythonService;

        public PredictionController(ApplicationDbContext db, IPythonPredictionService pythonService)
        {
            _db = db;
            _pythonService = pythonService;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new PredictionViewModel
            {
                Countries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(PredictionViewModel input)
        {
            input.Countries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync();

            if (input.CountryId.HasValue)
                input.Cities = await _db.Cities.Where(c => c.CountryId == input.CountryId).OrderBy(c => c.CityName).ToListAsync();

            if (!input.CountryId.HasValue)
            {
                input.ErrorMessage = "Please select a country.";
                return View("Index", input);
            }

            if (input.StartYear > input.EndYear)
            {
                input.ErrorMessage = "Start year must be less than or equal to end year.";
                return View("Index", input);
            }

            // Steps 1-6: PythonPredictionService retrieves + cleans historical data
            var result = await _pythonService.RunPredictionAsync(new PredictionRequest
            {
                CountryId = input.CountryId.Value,
                CityId = input.CityId,
                StartYear = input.StartYear,
                EndYear = input.EndYear
            });

            if (!result.Success)
            {
                input.ErrorMessage = result.ErrorMessage;
                return View("Index", input);
            }

            // Persist predictions to SQL Server, replacing any prior run
            // for the same Country/City/Model/Year so results stay current
            var existing = _db.Predictions.Where(p =>
                p.CountryId == input.CountryId &&
                p.CityId == input.CityId &&
                p.PredictionModel == result.Model &&
                p.Year >= input.StartYear && p.Year <= input.EndYear);

            _db.Predictions.RemoveRange(existing);

            var newPredictions = result.Predictions.Select(p => new Prediction
            {
                CountryId = input.CountryId!.Value,
                CityId = input.CityId,
                Year = p.Year,
                PredictedTotalBirths = p.TotalBirths,
                PredictedMaleBirths = p.MaleBirths,
                PredictedFemaleBirths = p.FemaleBirths,
                PredictionModel = result.Model,
                PredictionDate = DateTime.UtcNow
            }).ToList();

            _db.Predictions.AddRange(newPredictions);
            await _db.SaveChangesAsync();

            // Build combined Historical + Predicted table for display
            var historyQuery = _db.BirthRecords.AsNoTracking().Where(r => r.CountryId == input.CountryId);
            if (input.CityId.HasValue) historyQuery = historyQuery.Where(r => r.CityId == input.CityId);
            var history = await historyQuery.OrderBy(r => r.Year).ToListAsync();

            var rows = new List<PredictionRow>();
            rows.AddRange(history.Select(h => new PredictionRow
            {
                Year = h.Year,
                DataType = h.RecordType.ToString(),
                TotalBirths = h.TotalBirths,
                MaleBirths = h.MaleBirths,
                FemaleBirths = h.FemaleBirths
            }));
            rows.AddRange(newPredictions.Select(p => new PredictionRow
            {
                Year = p.Year,
                DataType = "Predicted",
                TotalBirths = p.PredictedTotalBirths,
                MaleBirths = p.PredictedMaleBirths,
                FemaleBirths = p.PredictedFemaleBirths
            }));

            input.CombinedRows = rows.OrderBy(r => r.Year).ToList();
            input.HasResults = true;

            TempData["Success"] = $"Prediction generated successfully using {result.Model}.";
            return View("Index", input);
        }
    }
}
