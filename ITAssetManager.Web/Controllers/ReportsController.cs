using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManager.Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> WarrantyExpiring()
        {
            // Get today's date
            var today = DateTime.Now.Date;

            // Get date 90 days from now
            var ninetyDaysFromNow = today.AddDays(90);

            // Query assets with warranties expiring in the next 90 days
            // NOTE: Using WarrantyExpiry (the actual DB column name)
            var expiringAssets = await _context.Assets
                .Include(a => a.Person)
                .Include(a => a.Location)
                .Where(a => a.WarrantyExpiry.HasValue
                         && a.WarrantyExpiry.Value >= today
                         && a.WarrantyExpiry.Value <= ninetyDaysFromNow)
                .OrderBy(a => a.WarrantyExpiry)
                .ToListAsync();

            ViewBag.ReportDate = today.ToString("MMMM dd, yyyy");
            ViewBag.ExpiringCount = expiringAssets.Count;

            return View(expiringAssets);
        }
    }
}