using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;

namespace ITAssetManager.Web.Controllers
{
    [Authorize] // All actions require authentication
    public class SoftwareProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SoftwareProductsController> _logger;

        public SoftwareProductsController(AppDbContext context, ILogger<SoftwareProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: SoftwareProducts
        // All authenticated users can view
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Index()
        {
            var products = await _context.SoftwareProducts
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(products);
        }

        // GET: SoftwareProducts/Details/5
        // All authenticated users can view details
        [Authorize(Policy = "AllUsers")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.SoftwareProducts
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: SoftwareProducts/Create
        // Only Administrators can create
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: SoftwareProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(SoftwareProduct product)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Software Product {Name} created by {User}",
                        product.Name, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Software product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating software product");
                    ModelState.AddModelError("", "An error occurred while creating the software product.");
                }
            }
            return View(product);
        }

        // GET: SoftwareProducts/Edit/5
        // Admin or Developer can edit
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.SoftwareProducts.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: SoftwareProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrDeveloper")]
        public async Task<IActionResult> Edit(int id, SoftwareProduct product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Software Product {Name} updated by {User}",
                        product.Name, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Software product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ProductExists(product.Id))
                        return NotFound();
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating product {Id}", id);
                        ModelState.AddModelError("", "The product was modified by another user.");
                    }
                }
            }
            return View(product);
        }

        // POST: SoftwareProducts/Delete/5
        // Only Administrators can delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.SoftwareProducts.FindAsync(id);
                if (product != null)
                {
                    _context.SoftwareProducts.Remove(product);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Software Product {Name} deleted by {User}",
                        product.Name, User.Identity?.Name);

                    TempData["SuccessMessage"] = "Software product deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting software product {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the software product.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id) => _context.SoftwareProducts.Any(e => e.Id == id);
    }
}