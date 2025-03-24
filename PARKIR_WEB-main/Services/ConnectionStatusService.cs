using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

public class ConnectionStatusService
{
    private readonly ILogger<ConnectionStatusService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ParkingHub> _hubContext;
    private bool _isConnected;

    public ConnectionStatusService(
        ILogger<ConnectionStatusService> logger,
        ApplicationDbContext context,
        IHubContext<ParkingHub> hubContext)
    {
        _logger = logger;
        _context = context;
        _hubContext = hubContext;
        _isConnected = true;
    }

    public async Task CheckConnection()
    {
        try
        {
            // Cek koneksi database lokal
            var dbConnected = await _context.Database.CanConnectAsync();
            
            // Cek koneksi printer (jika digunakan)
            var printerConnected = CheckPrinterConnection();
            
            // Cek koneksi kamera (jika digunakan)
            var cameraConnected = CheckCameraConnection();

            var allSystemsConnected = dbConnected && printerConnected && cameraConnected;

            if (!_isConnected && allSystemsConnected)
            {
                _isConnected = true;
                await _hubContext.Clients.All.SendAsync("SystemStatusChanged", new {
                    isConnected = true,
                    database = dbConnected,
                    printer = printerConnected,
                    camera = cameraConnected
                });
            }
            else if (_isConnected && !allSystemsConnected)
            {
                _isConnected = false;
                await _hubContext.Clients.All.SendAsync("SystemStatusChanged", new {
                    isConnected = false,
                    database = dbConnected,
                    printer = printerConnected,
                    camera = cameraConnected
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system connections");
            _isConnected = false;
            await _hubContext.Clients.All.SendAsync("SystemStatusChanged", new {
                isConnected = false,
                error = ex.Message
            });
        }
    }

    private bool CheckPrinterConnection()
    {
        // Implementasi cek printer lokal
        return true; // TODO: implement actual check
    }

    private bool CheckCameraConnection()
    {
        // Implementasi cek kamera lokal
        return true; // TODO: implement actual check
    }
} 