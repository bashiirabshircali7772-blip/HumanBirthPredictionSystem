using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize]
    public class CitiesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CitiesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search, int? countryId)
        {
            var query = _db.Cities.AsNoTracking().Include(c => c.Country).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CityName.Contains(search));

            if (countryId.HasValue)
                query = query.Where(c => c.CountryId == countryId);

            ViewBag.Search = search;
            ViewBag.CountryId = countryId;
            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName");

            var cities = await query.OrderBy(c => c.Country!.CountryName).ThenBy(c => c.CityName).ToListAsync();
            return View(cities);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName");
            return View(new City());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(City model)
        {
            if (await _db.Cities.AnyAsync(c => c.CountryId == model.CountryId && c.CityName == model.CityName))
                ModelState.AddModelError(nameof(model.CityName), "This city already exists for the selected country.");

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", model.CountryId);
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _db.Cities.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"City \"{model.CityName}\" was added successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var city = await _db.Cities.FindAsync(id);
            if (city == null) return NotFound();

            ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", city.CountryId);
            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, City model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = new SelectList(await _db.Countries.OrderBy(c => c.CountryName).ToListAsync(), "Id", "CountryName", model.CountryId);
                return View(model);
            }

            var existing = await _db.Cities.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CityName = model.CityName;
            existing.CountryId = model.CountryId;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"City \"{model.CityName}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _db.Cities.FindAsync(id);
            if (city == null) return NotFound();

            var hasRecords = await _db.BirthRecords.AnyAsync(r => r.CityId == id);
            if (hasRecords)
            {
                TempData["Error"] = "This city cannot be deleted because it has related birth records.";
                return RedirectToAction(nameof(Index));
            }

            _db.Cities.Remove(city);
            await _db.SaveChangesAsync();
            TempData["Success"] = "City deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Used by client-side JS to populate the City dropdown when a Country is selected
        [HttpGet]
        public async Task<JsonResult> GetByCountry(int countryId)
        {
            var cities = await _db.Cities
                .Where(c => c.CountryId == countryId)
                .OrderBy(c => c.CityName)
                .Select(c => new { c.Id, c.CityName })
                .ToListAsync();

            return Json(cities);
        }
    }
}
