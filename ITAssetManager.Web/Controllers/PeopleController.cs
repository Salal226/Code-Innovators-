using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;

namespace ITAssetManager.Web.Controllers
{
    [Authorize] // All actions require authentication
    public class PeopleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PeopleController> _logger;

        public PeopleController(AppDbContext context, ILogger<PeopleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: People
        // All authenticated users can view
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Index()
        {
            var people = await _context.People
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            return View(people);
        }

        // GET: People/Details/5
        // All authenticated users can view details
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var person = await _context.People
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null) return NotFound();

            return View(person);
        }

        // GET: People/Create
        // Only Administrators can create people
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: People/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Person person)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(person);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Person {Name} created by {User}",
                        person.FullName, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Person created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating person");
                    ModelState.AddModelError("", "An error occurred while creating the person.");
                }
            }
            return View(person);
        }

        // GET: People/Edit/5
        // Admin or Developer can edit
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var person = await _context.People.FindAsync(id);
            if (person == null) return NotFound();

            return View(person);
        }

        // POST: People/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int id, Person person)
        {
            if (id != person.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Person {Name} updated by {User}",
                        person.FullName, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Person updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!PersonExists(person.Id))
                        return NotFound();
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating person {Id}", id);
                        ModelState.AddModelError("", "The person was modified by another user.");
                    }
                }
            }
            return View(person);
        }

        // POST: People/Delete/5
        // Only Administrators can delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var person = await _context.People.FindAsync(id);
                if (person != null)
                {
                    // Check if person has any assets assigned
                    var assignedAssets = await _context.Assets
                        .Where(a => a.PersonId == id)
                        .CountAsync();

                    if (assignedAssets > 0)
                    {
                        _logger.LogWarning("Attempted to delete person {Name} with {Count} assigned assets by {User}",
                            person.FullName, assignedAssets, User.Identity?.Name);

                        TempData["ErrorMessage"] = $"Cannot delete {person.FullName} because they have {assignedAssets} asset(s) assigned to them. Please reassign or remove the assets first.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.People.Remove(person);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Person {Name} deleted by {User}",
                        person.FullName, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Person deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting person {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the person.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PersonExists(int id) => _context.People.Any(e => e.Id == id);
    }
}