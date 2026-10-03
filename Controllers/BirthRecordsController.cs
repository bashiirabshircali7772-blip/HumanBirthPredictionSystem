using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class BirthRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BirthRecordsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? countryId, int? cityId, int? year, RecordType? recordType, string? search)
        {
            var query = _db.BirthRecords.AsNoTracking()
                .Include(r => r.Country)
                .Include(r => r.City)
                .AsQueryable();

            if (countryId.HasValue) query = query.Where(r => r.CountryId == countryId);
            if (cityId.HasValue) query = query.Where(r => r.CityId == cityId);
            if (year.HasValue) query = query.Where(r => r.Year == year);
            if (recordType.HasValue) query = query.Where(r => r.RecordType == recordType);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(r => 
                    (r.Country != null && r.Country.CountryName.Contains(s)) ||
                    (r.City != null && r.City.CityName.Contains(s)) ||
                    r.DataSource.Contains(s));
            }

            var allCountries = await _db.Countries.OrderBy(c => c.CountryName).ToListAsync();
            ViewBag.Countries = new SelectList(allCountries, "Id", "CountryName", countryId);

            var citiesQuery = _db.Cities.AsNoTracking();
            if (countryId.HasValue)
            {
                citiesQuery = citiesQuery.Where(c => c.CountryId == countryId);
            }
            var allCities = await citiesQuery.OrderBy(c => c.CityName).ToListAsync();
            ViewBag.Cities = new SelectList(allCities, "Id", "CityName", cityId);

            ViewBag.CountryId = countryId;
            ViewBag.CityId = cityId;
            ViewBag.Year = year;
            ViewBag.RecordType = recordType;
            ViewBag.Search = search;

            var records = await query.OrderByDescending(r => r.Year).ThenBy(r => r.Country!.CountryName).ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Create()
        {
            var somalia = await GetSomaliaAsync();
            ViewBag.Countries = new SelectList(new[] { somalia }, "Id", "CountryName");
            ViewBag.Cities = new SelectList(await _db.Cities.Where(c => c.CountryId == somalia.Id).OrderBy(c => c.CityName).ToListAsync(), "Id", "CityName");
            return View(new BirthRecord { RecordType = RecordType.Historical, Year = DateTime.UtcNow.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BirthRecord model)
        {
            await ValidateRecordAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateSomaliaListsAsync(model.CountryId, model.CityId);
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            try
            {
                _db.BirthRecords.Add(model);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The record could not be saved. Check that the year and record type are not duplicated.");
                await PopulateSomaliaListsAsync(model.CountryId, model.CityId);
                return View(model);
            }

            TempData["Success"] = "Birth record was added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _db.BirthRecords.FindAsync(id);
            if (record == null) return NotFound();

            await PopulateSomaliaListsAsync(record.CountryId, record.CityId);
            return View(record);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BirthRecord model)
        {
            if (id != model.Id) return NotFound();

            await ValidateRecordAsync(model, id);

            if (!ModelState.IsValid)
            {
                await PopulateSomaliaListsAsync(model.CountryId, model.CityId);
                return View(model);
            }

            var existing = await _db.BirthRecords.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CountryId = model.CountryId;
            existing.CityId = model.CityId;
            existing.Year = model.Year;
            existing.TotalBirths = model.TotalBirths;
            existing.MaleBirths = model.MaleBirths;
            existing.FemaleBirths = model.FemaleBirths;
            existing.DataSource = model.DataSource;
            existing.SourceReference = model.SourceReference;
            existing.RecordType = model.RecordType;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The record could not be saved. Check that the year and record type are not duplicated.");
                await PopulateSomaliaListsAsync(model.CountryId, model.CityId);
                return View(model);
            }
            TempData["Success"] = "Birth record was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _db.BirthRecords.FindAsync(id);
            if (record == null) return NotFound();

            _db.BirthRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Birth record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Applies the business validation rules required by the thesis spec:
        /// MaleBirths + FemaleBirths must equal TotalBirths, values must not
        /// be negative, the City (if any) must belong to the selected
        /// Country, and duplicate Country/City/Year/RecordType combinations
        /// are prevented.
        /// </summary>
        private async Task ValidateRecordAsync(BirthRecord model, int? excludingId = null)
        {
            var somalia = await GetSomaliaAsync();
            if (model.CountryId != somalia.Id)
            {
                ModelState.AddModelError(nameof(model.CountryId), "Birth records can only be added for Somalia.");
            }

            if (model.MaleBirths + model.FemaleBirths != model.TotalBirths)
            {
                ModelState.AddModelError(string.Empty,
                    "Male births plus female births must equal total births.");
            }

            if (model.CityId.HasValue)
            {
                var cityBelongs = await _db.Cities.AnyAsync(c => c.Id == model.CityId && c.CountryId == model.CountryId);
                if (!cityBelongs)
                    ModelState.AddModelError(nameof(model.CityId), "The selected city does not belong to the selected country.");
            }

            var duplicateQuery = _db.BirthRecords.Where(r =>
                r.CountryId == model.CountryId &&
                r.CityId == model.CityId &&
                r.Year == model.Year &&
                r.RecordType == model.RecordType);

            if (excludingId.HasValue)
                duplicateQuery = duplicateQuery.Where(r => r.Id != excludingId);

            if (await duplicateQuery.AnyAsync())
            {
                ModelState.AddModelError(string.Empty,
                    "A record already exists for this country, city, year, and record type.");
            }
        }

        private Task<Country> GetSomaliaAsync()
        {
            return _db.Countries.SingleAsync(c => c.CountryCode == "SOM");
        }

        private async Task PopulateSomaliaListsAsync(int? countryId, int? cityId)
        {
            var somalia = await GetSomaliaAsync();
            ViewBag.Countries = new SelectList(new[] { somalia }, "Id", "CountryName", countryId);
            ViewBag.Cities = new SelectList(
                await _db.Cities.Where(c => c.CountryId == somalia.Id).OrderBy(c => c.CityName).ToListAsync(),
                "Id", "CityName", cityId);
        }
    }
}
