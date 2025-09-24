using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;






namespace ITAssetManager.Web.Controllers
{
    public class SoftwareLicensesController : Controller
    {
        private readonly AppDbContext _context;

        public SoftwareLicensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: SoftwareLicenses
        public async Task<IActionResult> Index()
        {
            var licenses = await _context.SoftwareLicenses
                .Include(s => s.Product)
                .OrderBy(s => s.Product!.Name)
                .ToListAsync();
            return View(licenses);
        }

        // GET: SoftwareLicenses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var softwareLicense = await _context.SoftwareLicenses
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (softwareLicense == null)
            {
                return NotFound();
            }

            return View(softwareLicense);
        }

        // GET: SoftwareLicenses/Create
        public IActionResult Create()
        {
            ViewData["SoftwareProductId"] = new SelectList(_context.SoftwareProducts, "Id", "Name");
            return View();
        }

        // POST: SoftwareLicenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SoftwareProductId,LicenseKey,PurchaseDate,ExpiryDate,SeatsPurchased,SeatsAssigned,Cost,Vendor,Notes")] SoftwareLicense softwareLicense)
        {
            if (ModelState.IsValid)
            {
                _context.Add(softwareLicense);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SoftwareProductId"] = new SelectList(_context.SoftwareProducts, "Id", "Name", softwareLicense.SoftwareProductId);
            return View(softwareLicense);
        }

        // GET: SoftwareLicenses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var softwareLicense = await _context.SoftwareLicenses.FindAsync(id);
            if (softwareLicense == null)
            {
                return NotFound();
            }
            ViewData["SoftwareProductId"] = new SelectList(_context.SoftwareProducts, "Id", "Name", softwareLicense.SoftwareProductId);
            return View(softwareLicense);
        }

        // POST: SoftwareLicenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SoftwareProductId,LicenseKey,PurchaseDate,ExpiryDate,SeatsPurchased,SeatsAssigned,Cost,Vendor,Notes")] SoftwareLicense softwareLicense)
        {
            if (id != softwareLicense.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(softwareLicense);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SoftwareLicenseExists(softwareLicense.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SoftwareProductId"] = new SelectList(_context.SoftwareProducts, "Id", "Name", softwareLicense.SoftwareProductId);
            return View(softwareLicense);
        }

        // GET: SoftwareLicenses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var softwareLicense = await _context.SoftwareLicenses
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (softwareLicense == null)
            {
                return NotFound();
            }

            return View(softwareLicense);
        }

        // POST: SoftwareLicenses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var softwareLicense = await _context.SoftwareLicenses.FindAsync(id);
            if (softwareLicense != null)
            {
                _context.SoftwareLicenses.Remove(softwareLicense);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SoftwareLicenseExists(int id)
        {
            return _context.SoftwareLicenses.Any(e => e.Id == id);
        }
    }
}