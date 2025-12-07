namespace ITAssetManager.Web.Services
{
    public interface IBackupService
    {
        Task<bool> CreateDatabaseBackupAsync(string backupPath = null);
        //Task<bool> RestoreDatabaseBackupAsync(string backupFilePath);
        Task<List<string>> GetBackupFilesAsync();
        Task<bool> DeleteOldBackupsAsync(int daysToKeep = 30);
    }
}
