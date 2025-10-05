using System.Diagnostics;
using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using ITAssetManager.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManager.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Dashboard (no lists here)
        public async Task<IActionResult> Index()
        {
            var cutoff = DateTime.Today.AddDays(30);

            var vm = new DashboardViewModel
            {
                TotalAssets = await _context.Assets.CountAsync(),
                InRepair = await _context.Assets.CountAsync(a => a.Status == "InRepair"),
                ExpiringWarranty30 = await _context.Assets.CountAsync(
                    a => a.WarrantyExpiry.HasValue && a.WarrantyExpiry.Value <= cutoff),
                SoftwareProducts = await _context.SoftwareProducts.CountAsync(),
                LicensesTotalSeats = await _context.SoftwareProducts.SumAsync(s => (int?)s.SeatCount) ?? 0,
                LicensesSeatsInUse = await _context.SoftwareProducts.SumAsync(s => (int?)s.SeatsInUse) ?? 0
            };


            return View(vm);
        }



        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestBackup()
        {
            try
            {
                var backupService = HttpContext.RequestServices.GetRequiredService<IBackupService>();
                var result = await backupService.CreateDatabaseBackupAsync();
        
                if (result)
                {
                    var backupFiles = await backupService.GetBackupFilesAsync();
                    return Content($" Backup created successfully! Found {backupFiles.Count} backup files.");
                }
                else
                {
                    return Content(" Backup failed!");
                }
            }
            catch (Exception ex)
            {
                return Content($" Error: {ex.Message}");
            }
        }
    }
}
