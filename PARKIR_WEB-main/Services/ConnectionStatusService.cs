using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Diagnostics;
using ParkIRC.Hubs;
using System.IO;
using Microsoft.Extensions.Configuration;
using ParkIRC.Data;

namespace ParkIRC.Services
{
    public class ConnectionStatusService : IHostedService, IDisposable
    {
        private readonly ILogger<ConnectionStatusService> _logger;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly PrintService _printService;
        private Timer _timer;
        private bool _isRunning = false;

        public ConnectionStatusService(
            ILogger<ConnectionStatusService> logger,
            IHubContext<ParkingHub> hubContext,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            PrintService printService)
        {
            _logger = logger;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _printService = printService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection Status Service is starting.");
            
            // Cek interval monitoring dari configuration (default 60 detik)
            int interval = _configuration.GetValue<int>("Monitoring:CheckInterval", 60);
            
            _timer = new Timer(CheckStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(interval));
            
            return Task.CompletedTask;
        }

        private async void CheckStatus(object state)
        {
            if (_isRunning) return;
            
            _isRunning = true;
            
            try
            {
                _logger.LogDebug("Checking system connection status...");
                
                var systemStatus = await GetSystemStatus();
                
                // Kirim status ke semua client
                await _hubContext.Clients.All.SendAsync("ReceiveSystemStatus", systemStatus);
                
                _logger.LogDebug($"System status: DB={systemStatus.Database}, Printer={systemStatus.Printer}, Camera={systemStatus.Camera}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system status");
            }
            finally
            {
                _isRunning = false;
            }
        }
        
        public async Task<SystemStatusDto> GetSystemStatus()
        {
            var status = new SystemStatusDto
            {
                Timestamp = DateTime.Now
            };
            
            // Check database connection
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                status.Database = await CheckDatabaseAsync(dbContext);
            }
            
            // Check printer
            status.Printer = CheckPrinter();
            
            // Check camera
            status.Camera = CheckCamera();
            
            // Get system resources
            GetSystemResources(status);
            
            return status;
        }

        private async Task<bool> CheckDatabaseAsync(ApplicationDbContext dbContext)
        {
            try
            {
                return await dbContext.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        private bool CheckPrinter()
        {
            try
            {
                // Check printer using CUPS
                var processInfo = new ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    return !string.IsNullOrEmpty(output) && !output.Contains("no destinations");
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CheckCamera()
        {
            try
            {
                // Implementasi check kamera sesuai dengan hardware yang digunakan
                // Contoh sederhana: cek apakah folder gambar bisa diakses
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                return Directory.Exists(uploadsPath) && Directory.GetAccessControl(uploadsPath) != null;
            }
            catch
            {
                return false;
            }
        }
        
        private void GetSystemResources(SystemStatusDto status)
        {
            try
            {
                // Get disk usage
                var diskProcess = new ProcessStartInfo
                {
                    FileName = "df",
                    Arguments = "-h .",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(diskProcess))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    if (lines.Length > 1)
                    {
                        var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 4)
                        {
                            status.DiskTotal = parts[1];
                            status.DiskUsed = parts[2];
                            status.DiskFree = parts[3];
                            status.DiskUsagePercent = parts[4];
                        }
                    }
                }
                
                // Get memory usage
                var memProcess = new ProcessStartInfo
                {
                    FileName = "free",
                    Arguments = "-h",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(memProcess))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    if (lines.Length > 1)
                    {
                        var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 6)
                        {
                            status.MemoryTotal = parts[1];
                            status.MemoryUsed = parts[2];
                            status.MemoryFree = parts[3];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system resources");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection Status Service is stopping.");
            
            _timer?.Change(Timeout.Infinite, 0);
            
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
    
    public class SystemStatusDto
    {
        public DateTime Timestamp { get; set; }
        public bool Database { get; set; }
        public bool Printer { get; set; }
        public bool Camera { get; set; }
        public bool IsConnected => Database && Printer && Camera;
        
        // System resources
        public string DiskTotal { get; set; }
        public string DiskUsed { get; set; }
        public string DiskFree { get; set; }
        public string DiskUsagePercent { get; set; }
        
        public string MemoryTotal { get; set; }
        public string MemoryUsed { get; set; }
        public string MemoryFree { get; set; }
    }
} 