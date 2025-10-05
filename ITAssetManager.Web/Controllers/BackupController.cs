namespace ITAssetManager.Web.Controllers
{
    using ITAssetManager.Web.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    //[Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        private readonly IBackupService _backupService;

        public BackupController(IBackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task<IActionResult> Index()
        {
            var backupFiles = await _backupService.GetBackupFilesAsync();
            return View(backupFiles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBackup()
        {
            var success = await _backupService.CreateDatabaseBackupAsync();

            if (success)
            {
                TempData["SuccessMessage"] = "Backup created successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create backup.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CleanupBackups()
        {
            var success = await _backupService.DeleteOldBackupsAsync();

            if (success)
            {
                TempData["SuccessMessage"] = "Old backups cleaned up successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to clean up old backups.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
