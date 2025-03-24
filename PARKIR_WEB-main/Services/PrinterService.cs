using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Extensions;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ParkIRC.Data;

namespace ParkIRC.Services
{
    public interface IPrinterService
    {
        Task<bool> PrintTicket(ParkingTicket ticket);
        Task<bool> PrintReceipt(ParkingTransaction transaction);
        bool CheckPrinterStatus();
        string GetDefaultPrinter();
        List<string> GetAvailablePrinters();
    }

    public class PrinterService : IPrinterService
    {
        private readonly ILogger<PrinterService> _logger;
        private readonly string _defaultPrinter;
        private const int GENERIC_WRITE = 0x40000000;
        private const int OPEN_EXISTING = 3;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private ClientWebSocket _webSocket;
        private bool _isConnected;
        private readonly string _printerWebSocketUrl;
        private readonly Dictionary<string, PrinterStatus> _printerStatuses;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        public PrinterService(
            ILogger<PrinterService> logger,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _printerWebSocketUrl = _configuration["PrinterSettings:WebSocketUrl"];
            _printerStatuses = new Dictionary<string, PrinterStatus>();
            _webSocket = new ClientWebSocket();
            if (OperatingSystem.IsWindows())
            {
                _defaultPrinter = GetDefaultPrinter();
            }
            else
            {
                _defaultPrinter = string.Empty;
                _logger.LogWarning("Printing is only supported on Windows. Using mock implementation for Linux.");
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Try WebSocket connection first
                await ConnectWebSocketAsync();

                // If WebSocket fails, try reading from JSON
                if (!_isConnected)
                {
                    await LoadPrinterConfigFromJsonAsync();
                }

                // If both fail, read from database
                if (!_printerStatuses.Any())
                {
                    await LoadPrinterConfigFromDatabaseAsync();
                }

                // Start monitoring printer status
                _ = StartPrinterMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing printer service");
            }
        }

        private async Task ConnectWebSocketAsync()
        {
            try
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    await _webSocket.ConnectAsync(new Uri(_printerWebSocketUrl), CancellationToken.None);
                    _isConnected = true;
                    _logger.LogInformation("WebSocket connected to printer successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to printer WebSocket");
                _isConnected = false;
            }
        }

        private async Task LoadPrinterConfigFromJsonAsync()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "printer_config.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = await File.ReadAllTextAsync(jsonPath);
                    var printers = JsonSerializer.Deserialize<List<PrinterConfig>>(jsonContent);
                    foreach (var printer in printers)
                    {
                        _printerStatuses[printer.Id] = new PrinterStatus
                        {
                            Id = printer.Id,
                            Name = printer.Name,
                            Port = printer.Port,
                            IsOnline = false,
                            LastChecked = DateTime.Now
                        };
                    }
                    _logger.LogInformation("Loaded printer config from JSON");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading printer config from JSON");
            }
        }

        private async Task LoadPrinterConfigFromDatabaseAsync()
        {
            try
            {
                var printers = await _context.PrinterConfigs
                    .Where(p => p.IsActive)
                    .ToListAsync();

                foreach (var printer in printers)
                {
                    _printerStatuses[printer.Id] = new PrinterStatus
                    {
                        Id = printer.Id,
                        Name = printer.Name,
                        Port = printer.Port,
                        IsOnline = false,
                        LastChecked = DateTime.Now
                    };
                }
                _logger.LogInformation("Loaded printer config from database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading printer config from database");
            }
        }

        private async Task StartPrinterMonitoringAsync()
        {
            while (true)
            {
                try
                {
                    if (_isConnected)
                    {
                        // Monitor via WebSocket
                        await MonitorPrinterViaWebSocketAsync();
                    }
                    else
                    {
                        // Monitor via Serial/USB
                        await MonitorPrinterViaSerialAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring printer status");
                }

                await Task.Delay(5000); // Check every 5 seconds
            }
        }

        private async Task MonitorPrinterViaWebSocketAsync()
        {
            var buffer = new byte[1024];
            while (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var status = JsonSerializer.Deserialize<PrinterStatus>(message);
                        UpdatePrinterStatus(status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving WebSocket message");
                    _isConnected = false;
                    break;
                }
            }
        }

        private async Task MonitorPrinterViaSerialAsync()
        {
            foreach (var printer in _printerStatuses.Values)
            {
                try
                {
                    using (var serialPort = new System.IO.Ports.SerialPort(printer.Port))
                    {
                        serialPort.Open();
                        printer.IsOnline = true;
                        printer.LastChecked = DateTime.Now;
                        serialPort.Close();
                    }
                }
                catch
                {
                    printer.IsOnline = false;
                    printer.LastChecked = DateTime.Now;
                }
            }
        }

        public async Task<bool> PrintTicket(ParkingTicket ticket)
        {
            if (!OperatingSystem.IsWindows())
            {
                // Linux mock implementation - just log the ticket info
                _logger.LogInformation("=== MOCK TICKET PRINTING (LINUX) ===");
                _logger.LogInformation($"Ticket Number: {ticket.TicketNumber}");
                _logger.LogInformation($"Date/Time: {ticket.IssueTime:dd/MM/yyyy HH:mm}");
                _logger.LogInformation($"Vehicle: {ticket.VehicleNumber}");
                _logger.LogInformation($"Vehicle Type: {ticket.VehicleType}");
                _logger.LogInformation($"Parking Space: {ticket.ParkingSpaceNumber}");
                _logger.LogInformation($"Entry Time: {ticket.EntryTime:dd/MM/yyyy HH:mm}");
                _logger.LogInformation("=== END MOCK TICKET PRINTING ===");
                return true; // Return success for Linux
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var pd = new PrintDocument();
                    pd.PrinterSettings.PrinterName = _defaultPrinter;

                    pd.PrintPage += (sender, e) =>
                    {
                        using var font = new Font("Arial", 10);
                        float yPos = 10;
                        e.Graphics.DrawString("TIKET PARKIR", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 10, yPos);
                        yPos += 20;

                        // Print ticket details
                        e.Graphics.DrawString($"No. Tiket: {ticket.TicketNumber}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Tanggal: {ticket.IssueTime:dd/MM/yyyy HH:mm}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Kendaraan: {ticket.VehicleNumber}", font, Brushes.Black, 10, yPos);
                        yPos += 20;

                        // Print QR Code if available
                        if (!string.IsNullOrEmpty(ticket.BarcodeImagePath))
                        {
                            try
                            {
                                using var qrImage = Image.FromFile(ticket.BarcodeImagePath);
                                e.Graphics.DrawImage(qrImage, 10, yPos, 100, 100);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error printing QR code");
                            }
                        }
                    };

                    pd.Print();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket");
                return false;
            }
        }

        public async Task<bool> PrintReceipt(ParkingTransaction transaction)
        {
            if (!OperatingSystem.IsWindows())
            {
                // Linux mock implementation - just log the receipt info
                _logger.LogInformation("=== MOCK RECEIPT PRINTING (LINUX) ===");
                _logger.LogInformation($"Transaction Number: {transaction.TransactionNumber}");
                _logger.LogInformation($"Vehicle: {transaction.Vehicle?.VehicleNumber ?? "-"}");
                _logger.LogInformation($"Entry: {transaction.EntryTime:dd/MM/yyyy HH:mm}");
                _logger.LogInformation($"Exit: {transaction.ExitTime:dd/MM/yyyy HH:mm}");
                
                // Check if ExitTime is not the default value
                if (transaction.ExitTime != default)
                {
                    _logger.LogInformation($"Duration: {(transaction.ExitTime - transaction.EntryTime):hh\\:mm}");
                }
                _logger.LogInformation($"Total: {transaction.TotalAmount.ToRupiah()}");
                _logger.LogInformation("=== END MOCK RECEIPT PRINTING ===");
                return true; // Return success for Linux
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var pd = new PrintDocument();
                    pd.PrinterSettings.PrinterName = _defaultPrinter;

                    pd.PrintPage += (sender, e) =>
                    {
                        using var font = new Font("Arial", 10);
                        float yPos = 10;
                        e.Graphics.DrawString("STRUK PARKIR", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 10, yPos);
                        yPos += 20;

                        // Print receipt details
                        e.Graphics.DrawString($"No. Transaksi: {transaction.TransactionNumber}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Kendaraan: {transaction.Vehicle?.VehicleNumber ?? "-"}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Masuk: {transaction.EntryTime:dd/MM/yyyy HH:mm}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Keluar: {transaction.ExitTime:dd/MM/yyyy HH:mm}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        
                        // Check if ExitTime is not the default value
                        if (transaction.ExitTime != default)
                        {
                            e.Graphics.DrawString($"Durasi: {(transaction.ExitTime - transaction.EntryTime):hh\\:mm}", font, Brushes.Black, 10, yPos);
                            yPos += 20;
                        }
                        e.Graphics.DrawString($"Total: {transaction.TotalAmount.ToRupiah()}", new Font("Arial", 10, FontStyle.Bold), Brushes.Black, 10, yPos);
                    };

                    pd.Print();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt");
                return false;
            }
        }

        public bool CheckPrinterStatus()
        {
            if (!OperatingSystem.IsWindows())
            {
                // Linux mock implementation - always return true
                _logger.LogInformation("Mock printer check on Linux - always returns true");
                return true;
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var handle = CreateFile($"\\\\.\\{_defaultPrinter}", GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (handle == IntPtr.Zero || handle.ToInt32() == -1)
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking printer status");
                return false;
            }
        }

        public string GetDefaultPrinter()
        {
            if (!OperatingSystem.IsWindows())
            {
                // Linux mock implementation - return mock printer name
                return "LINUX_MOCK_PRINTER";
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var ps = new PrinterSettings();
                    return ps.PrinterName;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default printer");
                return string.Empty;
            }
        }

        public List<string> GetAvailablePrinters()
        {
            if (!OperatingSystem.IsWindows())
            {
                // Linux mock implementation - return mock printer list
                return new List<string> { "LINUX_MOCK_PRINTER" };
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return PrinterSettings.InstalledPrinters.Cast<string>().ToList();
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available printers");
                return new List<string>();
            }
        }

        public async Task<bool> PrintTicket(string printerId, TicketData ticketData)
        {
            try
            {
                if (_isConnected)
                {
                    // Send via WebSocket
                    var message = JsonSerializer.Serialize(new
                    {
                        type = "print_ticket",
                        printerId,
                        data = ticketData
                    });
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                else
                {
                    // Print via Serial/USB
                    if (_printerStatuses.TryGetValue(printerId, out var printer) && printer.IsOnline)
                    {
                        using (var serialPort = new System.IO.Ports.SerialPort(printer.Port))
                        {
                            serialPort.Open();
                            string printData = FormatTicketData(ticketData);
                            await serialPort.WriteAsync(printData);
                            serialPort.Close();
                        }
                    }
                    else
                    {
                        throw new Exception($"Printer {printerId} is offline");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error printing ticket to printer {printerId}");
                return false;
            }
        }

        private void UpdatePrinterStatus(PrinterStatus status)
        {
            if (_printerStatuses.ContainsKey(status.Id))
            {
                _printerStatuses[status.Id] = status;
            }
        }

        public Dictionary<string, PrinterStatus> GetAllPrinterStatus()
        {
            return _printerStatuses;
        }
    }

    public class PrinterStatus
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Port { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastChecked { get; set; }
    }

    public class PrinterConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Port { get; set; }
        public bool IsActive { get; set; }
    }

    public class TicketData
    {
        public string TicketNumber { get; set; }
        public string VehicleNumber { get; set; }
        public string EntryTime { get; set; }
        public string Barcode { get; set; }
    }
} 