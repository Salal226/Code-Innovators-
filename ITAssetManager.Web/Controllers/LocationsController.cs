using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;

namespace ITAssetManager.Web.Controllers
{
    [Authorize] // All actions require authentication
    public class LocationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(AppDbContext context, ILogger<LocationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Locations
        // All authenticated users can view
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Index()
        {
            var locations = await _context.Locations
                .OrderBy(l => l.Building)
                .ThenBy(l => l.Room)
                .ToListAsync();

            return View(locations);
        }

        // GET: Locations/Details/5
        // All authenticated users can view details
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations
                .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null) return NotFound();

            return View(location);
        }

        // GET: Locations/Create
        // Only Administrators can create locations
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Locations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Location location)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(location);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Location {Building}-{Room} created by {User}",
                        location.Building, location.Room, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Location created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating location");
                    ModelState.AddModelError("", "An error occurred while creating the location.");
                }
            }
            return View(location);
        }

        // GET: Locations/Edit/5
        // Admin or Developer can edit
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            return View(location);
        }

        // POST: Locations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int id, Location location)
        {
            if (id != location.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(location);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Location {Building}-{Room} updated by {User}",
                        location.Building, location.Room, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Location updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!LocationExists(location.Id))
                        return NotFound();
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating location {Id}", id);
                        ModelState.AddModelError("", "The location was modified by another user.");
                    }
                }
            }
            return View(location);
        }

        // POST: Locations/Delete/5
        // Only Administrators can delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var location = await _context.Locations.FindAsync(id);
                if (location != null)
                {
                    _context.Locations.Remove(location);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Location {Building}-{Room} deleted by {User}",
                        location.Building, location.Room, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Location deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the location.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LocationExists(int id) => _context.Locations.Any(e => e.Id == id);
    }
}