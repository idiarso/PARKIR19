using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ParkIRC.Services;
using System.Linq;

namespace ParkIRC.Services
{
    public class SyncBackgroundService : BackgroundService
    {
        private readonly ILogger<SyncBackgroundService> _logger;
        private readonly LocalDatabaseService _localDatabaseService;
        private readonly TimeSpan _syncInterval;
        private readonly int _maxRetries = 3;
        private readonly TimeSpan _retryDelay = TimeSpan.FromMinutes(1);

        public SyncBackgroundService(
            ILogger<SyncBackgroundService> logger, 
            LocalDatabaseService localDatabaseService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = logger;
            _localDatabaseService = localDatabaseService;
            _syncInterval = TimeSpan.FromMinutes(
                configuration.GetValue<int>("SyncSettings:SyncIntervalMinutes", 15));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sync Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncWithRetryAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error during sync process");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        private async Task SyncWithRetryAsync(CancellationToken stoppingToken)
        {
            int retryCount = 0;
            bool syncSuccessful = false;

            while (!syncSuccessful && retryCount < _maxRetries && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        _logger.LogInformation($"Retry attempt {retryCount} of {_maxRetries}");
                        await Task.Delay(_retryDelay * retryCount, stoppingToken);
                    }

                    // Check if there are pending entries before attempting sync
                    var pendingEntries = await _localDatabaseService.GetPendingEntries();
                    if (!pendingEntries.Any())
                    {
                        _logger.LogDebug("No pending entries to sync");
                        return;
                    }

                    _logger.LogInformation($"Starting sync with {pendingEntries.Count()} pending entries");
                    await _localDatabaseService.SyncWithMainServer();
                    syncSuccessful = true;
                    _logger.LogInformation("Sync completed successfully");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= _maxRetries)
                    {
                        _logger.LogError(ex, $"Sync failed after {_maxRetries} attempts");
                        throw;
                    }
                    else
                    {
                        _logger.LogWarning(ex, $"Sync attempt {retryCount} failed, will retry");
                    }
                }
            }
        }
    }
} 