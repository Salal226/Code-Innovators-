using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using ITAssetManager.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManager.Web.Controllers
{
    public class SoftwareProductsController : Controller
    {
        private readonly AppDbContext _context;
        public SoftwareProductsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var items = await _context.SoftwareProducts
                .Select(p => new SoftwareProductListVm
                {
                    Id = p.Id,
                    Name = p.Name,
                    Vendor = p.Vendor ?? string.Empty,
                    Version = p.Version,
                    Publisher = p.Publisher,
                    Category = p.Category,

                    // If licenses exist -> aggregate from license rows
                    // Else -> fall back to product-level columns
                    Licenses = p.Licenses.Any()
                               ? p.Licenses.Count()
                               : (int?)p.SeatsPurchased ?? 0,

                    TotalSeats = p.Licenses.Any()
                                 ? (int)(p.Licenses.Sum(l => (int?)l.SeatsPurchased) ?? 0)
                                 : ((int?)p.TotalSeats ?? (int?)p.SeatCount ?? 0),

                    InUse = p.Licenses.Any()
                            ? (int)(p.Licenses.Sum(l => (int?)l.SeatsAssigned) ?? 0)
                            : ((int?)p.SeatsInUse ?? 0),

                    NextExpiry = p.Licenses
                                    .Where(l => l.ExpiryDate != null)
                                    .OrderBy(l => l.ExpiryDate)
                                    .Select(l => l.ExpiryDate)
                                    .FirstOrDefault()
                                 ?? p.LicenseExpiry
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(items);
        }


        // GET: /Software/Details/5  (product + licenses + assignments)
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.SoftwareProducts
                .Include(p => p.Licenses)
                .Include(p => p.Assignments).ThenInclude(a => a.Person)
                .Include(p => p.Assignments).ThenInclude(a => a.Asset)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Software/Create
        public IActionResult Create() => View();

        // POST: /Software/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Vendor,Version,UnitCost,Publisher,Category,LicenseKey,PurchaseDate,LicenseExpiry,SeatCount,SeatsPurchased,TotalSeats,SeatsInUse")]
            SoftwareProduct product)
        {
            if (!ModelState.IsValid) return View(product);

            _context.SoftwareProducts.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Software/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.SoftwareProducts.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Software/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Vendor,Version,UnitCost,Publisher,Category,LicenseKey,PurchaseDate,LicenseExpiry,SeatCount,SeatsPurchased,TotalSeats,SeatsInUse,SeatsAssigned")]
            SoftwareProduct product)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid) return View(product);

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.SoftwareProducts.Any(e => e.Id == product.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Software/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.SoftwareProducts
                .Include(p => p.Licenses)
                .Include(p => p.Assignments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Software/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.SoftwareProducts
                .Include(p => p.Assignments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // prevent deleting while there are active assignments
                if (product.Assignments.Any(a => a.IsActive))
                {
                    TempData["Error"] = "Cannot delete software product with active license assignments.";
                    return RedirectToAction(nameof(Index));
                }

                _context.SoftwareProducts.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
