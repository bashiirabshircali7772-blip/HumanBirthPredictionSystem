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
                query = query.Where(r => r.Country!.CountryName.Contains(search) || r.DataSource.Contains(search));

            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", countryId);
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
            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName");
            return View(new BirthRecord { RecordType = RecordType.Historical, Year = DateTime.UtcNow.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BirthRecord model)
        {
            await ValidateRecordAsync(model);

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", model.CountryId);
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _db.BirthRecords.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Birth record was added successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var record = await _db.BirthRecords.FindAsync(id);
            if (record == null) return NotFound();

            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", record.CountryId);
            ViewBag.Cities = new SelectList(await _db.Cities.Where(c => c.CountryId == record.CountryId).OrderBy(c => c.CityName).ToListAsync(), "Id", "CityName", record.CityId);
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BirthRecord model)
        {
            if (id != model.Id) return NotFound();

            await ValidateRecordAsync(model, id);

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", model.CountryId);
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

            await _db.SaveChangesAsync();
            TempData["Success"] = "Birth record was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
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
    }
}
