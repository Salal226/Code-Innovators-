using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;

namespace ITAssetManager.Web.Controllers
{
    [Authorize] // All actions require authentication
    public class SoftwareLicensesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SoftwareLicensesController> _logger;

        public SoftwareLicensesController(AppDbContext context, ILogger<SoftwareLicensesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: SoftwareLicenses
        // All authenticated users can view
        [Authorize(Policy = "CanViewAssets")]
        public async Task<IActionResult> Index()
        {
            var licenses = await _context.SoftwareLicenses
                .Include(l => l.Product)  // ✅ Include product info
                .OrderBy(l => l.LicenseKey)
                .ToListAsync();

            return View(licenses);
        }

        // GET: SoftwareLicenses/Details/5
        // All authenticated users can view details
        [Authorize(Policy = "CanViewAssets")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var license = await _context.SoftwareLicenses
                .Include(l => l.Product)  // ✅ Include product info
                .FirstOrDefaultAsync(m => m.Id == id);

            if (license == null) return NotFound();

            return View(license);
        }

        // GET: SoftwareLicenses/Create
        // Only Administrators can create licenses
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            // ✅ Populate dropdowns
            ViewBag.SoftwareProductId = new SelectList(
                await _context.SoftwareProducts.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name"
            );

            ViewBag.PersonId = new SelectList(
                await _context.People.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(),
                "Id",
                "FullName"
            );

            return View();
        }

        // POST: SoftwareLicenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(SoftwareLicense license, int? PersonId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ Save the license
                    _context.Add(license);
                    await _context.SaveChangesAsync();

                    // ✅ Create LicenseAssignment if a person was selected
                    if (PersonId.HasValue && PersonId.Value > 0)
                    {
                        var assignment = new LicenseAssignment
                        {
                            SoftwareProductId = license.SoftwareProductId,
                            PersonId = PersonId.Value,
                            AssignedOn = DateTime.Now,
                            IsActive = true
                        };
                        _context.LicenseAssignments.Add(assignment);
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Software License {LicenseKey} created by {User}",
                        license.LicenseKey, User.Identity?.Name);

                    TempData["SuccessMessage"] = "License created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating license");
                    ModelState.AddModelError("", "An error occurred while creating the license.");
                }
            }

            // ✅ Repopulate dropdowns if validation fails
            ViewBag.SoftwareProductId = new SelectList(
                await _context.SoftwareProducts.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name",
                license.SoftwareProductId
            );

            ViewBag.PersonId = new SelectList(
                await _context.People.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(),
                "Id",
                "FullName",
                PersonId
            );

            return View(license);
        }

        // GET: SoftwareLicenses/Edit/5
        // Admin or Developer can edit
        [Authorize(Policy = "CanManageAssets")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var license = await _context.SoftwareLicenses.FindAsync(id);
            if (license == null) return NotFound();

            // ✅ Populate dropdowns
            ViewBag.SoftwareProductId = new SelectList(
                await _context.SoftwareProducts.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name",
                license.SoftwareProductId
            );

            // ✅ Find existing assignment
            var existingAssignment = await _context.LicenseAssignments
                .FirstOrDefaultAsync(la => la.SoftwareProductId == license.SoftwareProductId && la.IsActive);

            ViewBag.PersonId = new SelectList(
                await _context.People.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(),
                "Id",
                "FullName",
                existingAssignment?.PersonId
            );

            return View(license);
        }

        // POST: SoftwareLicenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanManageAssets")]
        public async Task<IActionResult> Edit(int id, SoftwareLicense license, int? PersonId)
        {
            if (id != license.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(license);
                    await _context.SaveChangesAsync();

                    // ✅ Update assignment if needed
                    var existingAssignment = await _context.LicenseAssignments
                        .FirstOrDefaultAsync(la => la.SoftwareProductId == license.SoftwareProductId && la.IsActive);

                    if (PersonId.HasValue && PersonId.Value > 0)
                    {
                        if (existingAssignment != null)
                        {
                            // Update existing
                            existingAssignment.PersonId = PersonId.Value;
                            _context.Update(existingAssignment);
                        }
                        else
                        {
                            // Create new
                            var newAssignment = new LicenseAssignment
                            {
                                SoftwareProductId = license.SoftwareProductId,
                                PersonId = PersonId.Value,
                                AssignedOn = DateTime.Now,
                                IsActive = true
                            };
                            _context.Add(newAssignment);
                        }
                        await _context.SaveChangesAsync();
                    }
                    else if (existingAssignment != null)
                    {
                        // Remove assignment if person was cleared
                        existingAssignment.IsActive = false;
                        _context.Update(existingAssignment);
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Software License {LicenseKey} updated by {User}",
                        license.LicenseKey, User.Identity?.Name);

                    TempData["SuccessMessage"] = "License updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!LicenseExists(license.Id))
                        return NotFound();
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating license {Id}", id);
                        ModelState.AddModelError("", "The license was modified by another user.");
                    }
                }
            }

            // ✅ Repopulate dropdowns
            ViewBag.SoftwareProductId = new SelectList(
                await _context.SoftwareProducts.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name",
                license.SoftwareProductId
            );

            ViewBag.PersonId = new SelectList(
                await _context.People.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(),
                "Id",
                "FullName",
                PersonId
            );

            return View(license);
        }

        // POST: SoftwareLicenses/Delete/5
        // Only Administrators can delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var license = await _context.SoftwareLicenses.FindAsync(id);
                if (license != null)
                {
                    // ✅ Also deactivate any assignments
                    var assignments = await _context.LicenseAssignments
                        .Where(la => la.SoftwareProductId == license.SoftwareProductId)
                        .ToListAsync();

                    foreach (var assignment in assignments)
                    {
                        assignment.IsActive = false;
                    }

                    _context.SoftwareLicenses.Remove(license);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Software License {LicenseKey} deleted by {User}",
                        license.LicenseKey, User.Identity?.Name);

                    TempData["SuccessMessage"] = "License deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting license {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the license.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LicenseExists(int id) => _context.SoftwareLicenses.Any(e => e.Id == id);
    }
}