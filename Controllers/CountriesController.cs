using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class CountriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CountriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Countries?search=
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Countries.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CountryName.Contains(search) ||
                    c.CountryCode.Contains(search) ||
                    c.Continent.Contains(search));
            }

            ViewBag.Search = search;
            var countries = await query.OrderBy(c => c.CountryName).ToListAsync();
            return View(countries);
        }

        public IActionResult Create() => View(new Country());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Country model)
        {
            if (await _db.Countries.AnyAsync(c => c.CountryCode == model.CountryCode))
                ModelState.AddModelError(nameof(model.CountryCode), "This country code already exists.");

            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.UtcNow;
            _db.Countries.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Country \"{model.CountryName}\" was added successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var country = await _db.Countries.FindAsync(id);
            if (country == null) return NotFound();
            return View(country);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Country model)
        {
            if (id != model.Id) return NotFound();

            if (await _db.Countries.AnyAsync(c => c.CountryCode == model.CountryCode && c.Id != id))
                ModelState.AddModelError(nameof(model.CountryCode), "This country code already exists.");

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _db.Countries.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CountryName = model.CountryName;
            existing.CountryCode = model.CountryCode;
            existing.Continent = model.Continent;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Country \"{model.CountryName}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _db.Countries.FindAsync(id);
            if (country == null) return NotFound();

            var hasRecords = await _db.BirthRecords.AnyAsync(r => r.CountryId == id);
            var hasCities = await _db.Cities.AnyAsync(c => c.CountryId == id);

            if (hasRecords || hasCities)
            {
                TempData["Error"] = "This country cannot be deleted because it has related cities or birth records.";
                return RedirectToAction(nameof(Index));
            }

            _db.Countries.Remove(country);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Country deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
