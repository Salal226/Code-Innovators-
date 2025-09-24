using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using ITAssetManager.Web.Models.ViewModels;

namespace ITAssetManager.Web.Controllers
{
    public class AssetsController : Controller
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Assets
        public async Task<IActionResult> Index(string? q, string sort = "tag", int page = 1, int pageSize = 10)
        {
            var query = _context.Assets
                .AsNoTracking()
                .Include(a => a.Person)     // mapped navs only
                .Include(a => a.Location)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim();
                query = query.Where(a =>
                       (a.AssetTag ?? "").Contains(qq)
                    || (a.Model ?? "").Contains(qq)
                    || (a.SerialNumber ?? "").Contains(qq)
                    || ((a.Person != null
                         ? ((a.Person.FirstName ?? "") + " " + (a.Person.LastName ?? ""))
                         : ""))
                       .Contains(qq));
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

            return View(items);
        }

        // GET: Assets/Details/5
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
        public async Task<IActionResult> Create()
        {
            var vm = new AssetCreateViewModel();

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
            ViewBag.PersonId = new SelectList(people, "Id", "Display");

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
            ViewBag.LocationId = new SelectList(locations, "Id", "Display");

            return View(vm);
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssetCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var peopleAgain = await _context.People
                    .AsNoTracking()
                    .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                    .Select(p => new
                    {
                        p.Id,
                        Display = (p.FirstName ?? "") +
                                  (string.IsNullOrEmpty(p.LastName) ? "" : " " + p.LastName)
                    })
                    .ToListAsync();
                ViewBag.PersonId = new SelectList(peopleAgain, "Id", "Display", vm.AssignedToPersonId);

                var locs = await _context.Locations
                    .AsNoTracking()
                    .OrderBy(l => l.Building).ThenBy(l => l.Room)
                    .Select(l => new
                    {
                        l.Id,
                        Display = l.Building + " - " + l.Room +
                                  (l.LabNumber != null ? " (Lab " + l.LabNumber + ")" : "")
                    })
                    .ToListAsync();
                ViewBag.LocationId = new SelectList(locs, "Id", "Display", vm.LocationId);

                return View(vm);
            }

            // create Person if free-typed
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

            // create Location if free-typed
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
                WarrantyEnd = vm.WarrantyEnd,   // alias → mapped WarrantyExpiry
                Status = vm.Status,
                AssignedToPersonId = personId,
                LocationId = locationId
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Assets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();

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
            ViewBag.PersonId = new SelectList(people, "Id", "Display", asset.AssignedToPersonId);

            var locations = await _context.Locations
                .AsNoTracking()
                .Select(l => new { l.Id, Display = l.Building + " - " + l.Room })
                .OrderBy(l => l.Display)
                .ToListAsync();
            ViewBag.LocationId = new SelectList(locations, "Id", "Display", asset.LocationId);

            return View(asset);
        }

        // POST: Assets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assets.Any(e => e.Id == asset.Id))
                        return NotFound();
                    throw;
                }
            }

            // repopulate on error and return the edit view
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
            ViewBag.PersonId = new SelectList(people, "Id", "Display", asset.AssignedToPersonId);

            var locations = await _context.Locations
                .AsNoTracking()
                .Select(l => new { l.Id, Display = l.Building + " - " + l.Room })
                .OrderBy(l => l.Display)
                .ToListAsync();
            ViewBag.LocationId = new SelectList(locations, "Id", "Display", asset.LocationId);

            return View(asset);
        }

        // GET: Assets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .AsNoTracking()
                .Include(a => a.Person)    // mapped navs
                .Include(a => a.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            return View(asset);
        }

        // POST: Assets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset != null)
            {
                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AssetExists(int id) => _context.Assets.Any(e => e.Id == id);
    }
}
