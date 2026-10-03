using HumanBirthPredictionSystem.Data;
using HumanBirthPredictionSystem.Models;
using HumanBirthPredictionSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Users
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(user =>
                    user.Username.Contains(search) ||
                    user.FullName.Contains(search) ||
                    user.Role.Contains(search));
            }

            ViewBag.Search = search;
            return View(await query.OrderBy(user => user.Id).ToListAsync());
        }

        // GET: /Users/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var trimmedUsername = model.Username.Trim();
            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == trimmedUsername.ToLower()))
            {
                ModelState.AddModelError("Username", "This username is already taken. Please choose another.");
                return View(model);
            }

            var user = new User
            {
                Username = trimmedUsername,
                FullName = model.FullName.Trim(),
                Role = model.Role,
                PasswordHash = PasswordHasher.Hash(model.Password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };

            return View(vm);
        }

        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var trimmedUsername = model.Username.Trim();
            if (await _db.Users.AnyAsync(u => u.Id != id && u.Username.ToLower() == trimmedUsername.ToLower()))
            {
                ModelState.AddModelError("Username", "This username is already in use by another user.");
                return View(model);
            }

            user.Username = trimmedUsername;
            user.FullName = model.FullName.Trim();
            user.Role = model.Role;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' details updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentUsername = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
            if (string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Index));
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"User '{user.Username}' was removed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}