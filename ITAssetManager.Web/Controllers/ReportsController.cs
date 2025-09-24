using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManager.Web.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult WarrantyExpiring()
        {
            // Return empty list for testing
            return View(new List<Asset>());
        }
    }
}