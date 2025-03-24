using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;

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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        public PrinterService(ILogger<PrinterService> logger)
        {
            _logger = logger;
            if (OperatingSystem.IsWindows())
            {
                _defaultPrinter = GetDefaultPrinter();
            }
            else
            {
                _defaultPrinter = string.Empty;
                _logger.LogWarning("Printing is only supported on Windows");
            }
        }

        public async Task<bool> PrintTicket(ParkingTicket ticket)
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogWarning("Printing is only supported on Windows");
                return false;
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
                        e.Graphics.DrawString($"Kendaraan: {ticket.Vehicle?.VehicleNumber ?? "-"}", font, Brushes.Black, 10, yPos);
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
                _logger.LogWarning("Printing is only supported on Windows");
                return false;
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
                        e.Graphics.DrawString($"Durasi: {(transaction.ExitTime - transaction.EntryTime):hh\\:mm}", font, Brushes.Black, 10, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Total: Rp {transaction.TotalAmount:N0}", new Font("Arial", 10, FontStyle.Bold), Brushes.Black, 10, yPos);
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
                _logger.LogWarning("Printing is only supported on Windows");
                return false;
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
                _logger.LogWarning("Printing is only supported on Windows");
                return string.Empty;
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
                _logger.LogWarning("Printing is only supported on Windows");
                return new List<string>();
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
    }
} 