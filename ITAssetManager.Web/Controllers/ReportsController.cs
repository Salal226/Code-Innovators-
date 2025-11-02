using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            try
            {
                // Calculate date range for warranties expiring in next 30 days
                var startDate = DateTime.Today;
                var endDate = DateTime.Today.AddDays(30);

                // Query assets with warranties expiring in the next 30 days
                var expiringAssets = await _context.Assets
                    .Where(a => a.WarrantyExpiry != null &&
                                a.WarrantyExpiry >= startDate &&
                                a.WarrantyExpiry <= endDate)
                    .OrderBy(a => a.WarrantyExpiry)
                    .ToListAsync();

                return View(expiringAssets);
            }
            catch (Exception ex)
            {
                // Log error and return empty list for safety
                // You might want to add proper logging here
                Console.WriteLine($"Error retrieving warranty data: {ex.Message}");
                return View(new List<Asset>());
            }
        }
    }
}
