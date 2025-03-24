using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ParkIRC.Services
{
    public class ScheduledBackupService : BackgroundService
    {
        private readonly ILogger<ScheduledBackupService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly string _backupDir;
        private Timer _timer;

        public ScheduledBackupService(
            ILogger<ScheduledBackupService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
            
            // Create backup directory if it doesn't exist
            if (!Directory.Exists(_backupDir))
            {
                Directory.CreateDirectory(_backupDir);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Backup Service is starting");
            
            // Get backup schedule from configuration (default: 3 AM daily)
            var scheduledTime = _configuration.GetValue<string>("Backup:ScheduledTime", "03:00");
            var keepBackups = _configuration.GetValue<int>("Backup:KeepDays", 7);
            
            // Calculate time until next scheduled backup
            var timeSpan = CalculateTimeUntilNextBackup(scheduledTime);
            
            _logger.LogInformation($"Next scheduled backup in {timeSpan.TotalHours:F1} hours");
            
            _timer = new Timer(DoBackup, keepBackups, timeSpan, TimeSpan.FromHours(24));
            
            return Task.CompletedTask;
        }

        private TimeSpan CalculateTimeUntilNextBackup(string scheduledTime)
        {
            var timeParts = scheduledTime.Split(':');
            var hour = int.Parse(timeParts[0]);
            var minute = int.Parse(timeParts[1]);
            
            var now = DateTime.Now;
            var scheduledDateTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            
            // If the scheduled time has already passed today, set for tomorrow
            if (now > scheduledDateTime)
            {
                scheduledDateTime = scheduledDateTime.AddDays(1);
            }
            
            return scheduledDateTime - now;
        }

        private async void DoBackup(object state)
        {
            try
            {
                _logger.LogInformation("Starting scheduled backup...");
                
                int keepDays = (int)state;
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"backup_{timestamp}.zip";
                string backupPath = Path.Combine(_backupDir, backupFileName);
                string logPath = Path.Combine(_backupDir, $"backup_log_{timestamp}.txt");
                
                // Perform the database backup
                using (StreamWriter writer = new StreamWriter(logPath))
                {
                    writer.WriteLine($"Scheduled backup started at {DateTime.Now}");
                    
                    // Backup database
                    writer.WriteLine("Backing up database...");
                    string tempDbPath = Path.Combine(Path.GetTempPath(), $"parkir_db_{timestamp}.dump");
                    
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "pg_dump",
                        Arguments = $"-Fc parkir_db > {tempDbPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using (var process = Process.Start(processInfo))
                    {
                        process.WaitForExit();
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        
                        if (process.ExitCode != 0)
                        {
                            writer.WriteLine($"Error backing up database: {error}");
                            _logger.LogError($"Database backup failed: {error}");
                            return;
                        }
                    }
                    
                    // Backup uploads
                    writer.WriteLine("Backing up uploads...");
                    string uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "uploads");
                    
                    // Create the zip file
                    writer.WriteLine("Creating backup archive...");
                    using (var zip = System.IO.Compression.ZipFile.Open(backupPath, System.IO.Compression.ZipArchiveMode.Create))
                    {
                        // Add database dump to zip
                        zip.CreateEntryFromFile(tempDbPath, "database.dump");
                        
                        // Add uploads to zip if the directory exists
                        if (Directory.Exists(uploadsPath))
                        {
                            foreach (string file in Directory.GetFiles(uploadsPath, "*", SearchOption.AllDirectories))
                            {
                                string relativePath = file.Substring(uploadsPath.Length + 1);
                                zip.CreateEntryFromFile(file, Path.Combine("uploads", relativePath));
                            }
                        }
                        
                        // Add config file
                        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                        if (File.Exists(configPath))
                        {
                            zip.CreateEntryFromFile(configPath, "appsettings.json");
                        }
                    }
                    
                    // Cleanup temp files
                    File.Delete(tempDbPath);
                    
                    // Cleanup old backups
                    writer.WriteLine($"Cleaning up backups older than {keepDays} days...");
                    foreach (string file in Directory.GetFiles(_backupDir, "backup_*.zip"))
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.AddDays(-keepDays))
                        {
                            File.Delete(file);
                            writer.WriteLine($"Deleted old backup: {fi.Name}");
                        }
                    }
                    
                    writer.WriteLine($"Backup completed at {DateTime.Now}");
                    _logger.LogInformation($"Scheduled backup completed: {backupFileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing scheduled backup");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Scheduled Backup Service is stopping");
            
            _timer?.Change(Timeout.Infinite, 0);
            
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
} 