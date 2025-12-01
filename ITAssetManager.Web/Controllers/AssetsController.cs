using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using ITAssetManager.Web.Models.ViewModels;

namespace ITAssetManager.Web.Controllers
{
    [Authorize] // All actions require authentication
    public class AssetsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(AppDbContext context, ILogger<AssetsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Assets
        // All authenticated users can view the list
        [Authorize(Policy = "CanViewAssets")]
        public async Task<IActionResult> Index(string? q, string sort = "tag", int page = 1, int pageSize = 10)
        {
            var query = _context.Assets
                .AsNoTracking()
                .Include(a => a.Person)
                .Include(a => a.Location)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim().ToLower(); // ✅ CHANGED: Added ToLower() for case-insensitive search
                query = query.Where(a =>
                       (a.AssetTag ?? "").ToLower().Contains(qq)
                    || (a.Model ?? "").ToLower().Contains(qq)
                    || (a.SerialNumber ?? "").ToLower().Contains(qq)
                    || (a.Status ?? "").ToLower().Contains(qq)  // ✅ NEW: Added Status search
                    || ((a.Person != null
                         ? ((a.Person.FirstName ?? "") + " " + (a.Person.LastName ?? ""))
                         : ""))
                       .ToLower().Contains(qq));  // ✅ CHANGED: Added ToLower()
            }

            query = sort switch
            {
                "model" => query.OrderBy(a => a.Model),
                "status" => query.OrderBy(a => a.Status),
                _ => query.OrderBy(a => a.AssetTag)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Q = q;
            ViewBag.Sort = sort;

            _logger.LogInformation("Assets Index viewed by {User}. Total: {Total}, Page: {Page}",
                User.Identity?.Name, total, page);

            return View(items);
        }

        // GET: Assets/Details/5
        // All authenticated users can view details
        [Authorize(Policy = "CanViewAssets")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .AsNoTracking()
                .Include(a => a.Person)
                .Include(a => a.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            return View(asset);
        }

        // GET: Assets/Create
        // Only Administrators can create assets
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()  // ✅ CORRECT
        {
            var vm = new AssetCreateViewModel();

            ViewBag.AssignedToPersonId = await GetPeopleSelectList();
            ViewBag.LocationId = await GetLocationsSelectList();

            return View(vm);
        }

        // POST: Assets/Create
        // Only Administrators can create assets
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(AssetCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AssignedToPersonId = await GetPeopleSelectList(vm.AssignedToPersonId);
                ViewBag.LocationId = await GetLocationsSelectList(vm.LocationId);
                return View(vm);
            }

            try
            {
                // Create Person if free-typed
                int? personId = vm.AssignedToPersonId;
                if (personId == null && !string.IsNullOrWhiteSpace(vm.NewPersonName))
                {
                    var person = new Person
                    {
                        FullName = vm.NewPersonName.Trim(),
                        Email = vm.NewPersonEmail?.Trim() ?? ""
                    };
                    _context.People.Add(person);
                    await _context.SaveChangesAsync();
                    personId = person.Id;
                }

                // Create Location if free-typed
                int? locationId = vm.LocationId;
                bool hasNewLocation =
                       !string.IsNullOrWhiteSpace(vm.NewLocationBuilding)
                    || !string.IsNullOrWhiteSpace(vm.NewLocationRoom)
                    || !string.IsNullOrWhiteSpace(vm.NewLocationLabNumber);

                if (locationId == null && hasNewLocation)
                {
                    var location = new Location
                    {
                        Building = vm.NewLocationBuilding?.Trim() ?? "",
                        Room = vm.NewLocationRoom?.Trim() ?? "",
                        LabNumber = string.IsNullOrWhiteSpace(vm.NewLocationLabNumber)
                                    ? null
                                    : vm.NewLocationLabNumber.Trim()
                    };
                    _context.Locations.Add(location);
                    await _context.SaveChangesAsync();
                    locationId = location.Id;
                }

                var asset = new Asset
                {
                    AssetTag = vm.AssetTag,
                    Category = vm.Category,
                    Model = vm.Model,
                    SerialNumber = vm.SerialNumber,
                    PurchaseDate = vm.PurchaseDate,
                    PurchaseCost = vm.PurchaseCost,
                    WarrantyEnd = vm.WarrantyEnd,
                    Status = vm.Status,
                    AssignedToPersonId = personId,
                    LocationId = locationId
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Asset {AssetTag} created by {User}",
                    asset.AssetTag, User.Identity?.Name);

                TempData["SuccessMessage"] = "Asset created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating asset");
                ModelState.AddModelError("", "An error occurred while creating the asset.");

                ViewBag.AssignedToPersonId = await GetPeopleSelectList(vm.AssignedToPersonId);
                ViewBag.LocationId = await GetLocationsSelectList(vm.LocationId);
                return View(vm);
            }
        }

        // GET: Assets/Edit/5
        // Admin or Developer can edit
        [Authorize(Policy = "CanManageAssets")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();

            ViewBag.AssignedToPersonId = await GetPeopleSelectList(asset.AssignedToPersonId);
            ViewBag.LocationId = await GetLocationsSelectList(asset.LocationId);

            return View(asset);
        }

        // POST: Assets/Edit/5
        // Admin or Developer can edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanManageAssets")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,AssetTag,Category,Model,SerialNumber,PurchaseDate,PurchaseCost,WarrantyEnd,Status,AssignedToPersonId,LocationId")]
            Asset asset)
        {
            if (id != asset.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(asset);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Asset {AssetTag} updated by {User}",
                        asset.AssetTag, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Asset updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!AssetExists(asset.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating asset {Id}", id);
                        ModelState.AddModelError("", "The asset was modified by another user. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating asset {Id}", id);
                    ModelState.AddModelError("", "An error occurred while updating the asset.");
                }
            }

            // Repopulate dropdowns on validation error
            ViewBag.AssignedToPersonId = await GetPeopleSelectList(asset.AssignedToPersonId);
            ViewBag.LocationId = await GetLocationsSelectList(asset.LocationId);

            return View(asset);
        }

        // GET: Assets/Delete/5
        // Only Administrators can delete
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .AsNoTracking()
                .Include(a => a.Person)
                .Include(a => a.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            return View(asset);
        }

        // POST: Assets/Delete/5
        // Only Administrators can delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var asset = await _context.Assets.FindAsync(id);
                if (asset != null)
                {
                    var assetTag = asset.AssetTag;
                    _context.Assets.Remove(asset);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Asset {AssetTag} deleted by {User}",
                        assetTag, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Asset deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting asset {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the asset.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to populate People dropdown
        private async Task<SelectList> GetPeopleSelectList(int? selectedId = null)
        {
            var people = await _context.People
                .AsNoTracking()
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new
                {
                    p.Id,
                    Display = (p.FirstName ?? "") +
                              (string.IsNullOrEmpty(p.LastName) ? "" : " " + p.LastName)
                })
                .ToListAsync();

            return new SelectList(people, "Id", "Display", selectedId);
        }

        // Helper method to populate Locations dropdown
        private async Task<SelectList> GetLocationsSelectList(int? selectedId = null)
        {
            var locations = await _context.Locations
                .AsNoTracking()
                .OrderBy(l => l.Building).ThenBy(l => l.Room)
                .Select(l => new
                {
                    l.Id,
                    Display = l.Building + " - " + l.Room +
                              (l.LabNumber != null ? " (Lab " + l.LabNumber + ")" : "")
                })
                .ToListAsync();

            return new SelectList(locations, "Id", "Display", selectedId);
        }

        private bool AssetExists(int id) => _context.Assets.Any(e => e.Id == id);
    }
}