using ITAssetManager.Web.Services;
using Microsoft.Data.SqlClient;

namespace ITAssetManager.Web.Services
{
    public class SqlBackupService : IBackupService
    {
        private readonly IConfiguration _configuration;
        private readonly string _backupBasePath;
        private readonly ILogger<SqlBackupService> _logger;

        public SqlBackupService(IConfiguration configuration, ILogger<SqlBackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _backupBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        }

        public async Task<bool> CreateDatabaseBackupAsync(string backupPath = null)
        {
            try
            {
                backupPath ??= _backupBasePath;

                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var databaseName = GetDatabaseNameFromConnectionString(connectionString);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_Backup_{timestamp}.bak";
                var fullBackupPath = Path.Combine(backupPath, backupFileName);

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var backupCommand = $@"
                BACKUP DATABASE [{databaseName}] 
                TO DISK = '{fullBackupPath}' 
                WITH FORMAT, 
                     MEDIANAME = 'SQLServerBackups', 
                     NAME = 'Full Backup of {databaseName}';";

                using var command = new SqlCommand(backupCommand, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation($"Database backup created successfully: {backupFileName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database backup");
                return false;
            }
        }

        public Task<List<string>> GetBackupFilesAsync()
        {
            var backupFiles = new List<string>();

            if (Directory.Exists(_backupBasePath))
            {
                backupFiles.AddRange(Directory.GetFiles(_backupBasePath, "*.bak"));
            }

            return Task.FromResult(backupFiles);
        }

        public Task<bool> DeleteOldBackupsAsync(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(_backupBasePath))
                    return Task.FromResult(true);

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var backupFiles = Directory.GetFiles(_backupBasePath, "*.bak");

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                        _logger.LogInformation($"Deleted old backup: {fileInfo.Name}");
                    }
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old backups");
                return Task.FromResult(false);
            }
        }

        private string GetDatabaseNameFromConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog;
        }
    }
}
