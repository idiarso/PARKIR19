using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ParkIRC.Services
{
    public class PrintService
    {
        private readonly ILogger<PrintService> _logger;
        private readonly IConfiguration _config;
        private readonly string _printerName;
        private readonly string _driverName;

        public PrintService(ILogger<PrintService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _printerName = config["Printer:DefaultName"] ?? "EPSON_TM_T82";
            _driverName = config["Printer:Driver"] ?? "epson-escpos";
        }

        public bool PrintTicket(string content)
        {
            try
            {
                // Implementasi sesuai dengan printer yang digunakan
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return PrintWindows(content);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return PrintLinux(content);
                }
                
                throw new PlatformNotSupportedException("Platform tidak didukung");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket");
                return false;
            }
        }

        private bool PrintWindows(string content)
        {
            // Implementasi untuk Windows menggunakan Raw Printing
            // ...
            return true;
        }

        private bool PrintLinux(string content)
        {
            try
            {
                // Tambahkan ESC/POS commands untuk format printer thermal
                var escposCommands = new StringBuilder();
                
                // Initialize printer
                escposCommands.Append("\x1B\x40"); // ESC @ - Initialize printer
                
                // Set print mode
                escposCommands.Append("\x1B\x21\x00"); // ESC ! 0 - Normal print mode
                
                // Add content
                escposCommands.Append(content);
                
                // Cut paper
                if (_config.GetValue<bool>("Printer:AutoCut"))
                {
                    escposCommands.Append("\x1D\x56\x41\x00"); // GS V A 0 - Full cut
                }
                
                // Open cash drawer if enabled
                if (_config.GetValue<bool>("Printer:CashDrawer:Enabled"))
                {
                    escposCommands.Append("\x1B\x70\x00\x19\x78"); // ESC p 0 25 120 - Pulse on pin 2
                }

                // Kirim ke printer menggunakan lp command
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "lp",
                        Arguments = $"-d {_printerName} -o raw",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.StandardInput.Write(escposCommands.ToString());
                process.StandardInput.Close();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError($"Printing failed: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing on Linux");
                return false;
            }
        }
    }
} 