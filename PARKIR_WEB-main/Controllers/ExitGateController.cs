using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using ParkIRC.Hubs;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ParkIRC.Controllers
{
    [Authorize(Roles = "Operator")]
    public class ExitGateController : Controller
    {
        private readonly ILogger<ExitGateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PrintService _printService;
        private readonly IHubContext<ParkingHub> _hubContext;

        public ExitGateController(
            ILogger<ExitGateController> logger,
            ApplicationDbContext context,
            PrintService printService,
            IHubContext<ParkingHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _printService = printService;
            _hubContext = hubContext;
        }

        // View utama untuk pintu keluar - defaultnya scan barcode
        public IActionResult Index()
        {
            ViewBag.DefaultMode = "barcode";
            return View();
        }

        // Process barcode dari scanner
        [HttpPost]
        public async Task<IActionResult> ProcessBarcode(string barcode)
        {
            try
            {
                if (string.IsNullOrEmpty(barcode))
                {
                    return Json(new { success = false, message = "Barcode tidak valid" });
                }

                // Cari transaksi berdasarkan barcode
                var transaction = await _context.ParkingTransactions
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TicketNumber == barcode && t.ExitTime == default);

                if (transaction == null)
                {
                    return Json(new { success = false, message = "Tiket tidak ditemukan atau sudah digunakan" });
                }

                // Hitung durasi dan biaya
                var exitTime = DateTime.Now;
                var duration = exitTime - transaction.EntryTime;
                
                // Update transaksi
                transaction.ExitTime = exitTime;
                transaction.Duration = duration;
                // Hitung biaya sesuai tarif
                transaction.TotalAmount = CalculateFee(transaction.Vehicle.VehicleType, duration);
                
                await _context.SaveChangesAsync();
                
                // Buka gate
                await _hubContext.Clients.All.SendAsync("OpenExitGate", transaction.ParkingSpace);
                
                // Catat log
                _logger.LogInformation($"Exit gate opened for vehicle {transaction.Vehicle.VehicleNumber} with barcode {barcode}");
                
                // Return data untuk tampilan receipt
                return Json(new { 
                    success = true, 
                    vehicleNumber = transaction.Vehicle.VehicleNumber,
                    vehicleType = transaction.Vehicle.VehicleType,
                    entryTime = transaction.EntryTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    exitTime = exitTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    duration = FormatDuration(duration),
                    amount = transaction.TotalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing barcode");
                return Json(new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // Find by vehicle number (alternative method)
        [HttpPost]
        public async Task<IActionResult> FindByVehicleNumber(string vehicleNumber)
        {
            if (string.IsNullOrEmpty(vehicleNumber))
            {
                return Json(new { success = false, message = "Nomor kendaraan tidak valid" });
            }

            var transaction = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.Vehicle.VehicleNumber == vehicleNumber && t.ExitTime == default);

            if (transaction == null)
            {
                return Json(new { success = false, message = "Kendaraan tidak ditemukan atau sudah keluar" });
            }

            return Json(new { 
                success = true, 
                ticketNumber = transaction.TicketNumber,
                vehicleNumber = transaction.Vehicle.VehicleNumber,
                vehicleType = transaction.Vehicle.VehicleType,
                entryTime = transaction.EntryTime.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        private decimal CalculateFee(string vehicleType, TimeSpan duration)
        {
            // Implementasi perhitungan tarif sesuai jenis kendaraan dan durasi
            // Contoh sederhana
            decimal hourlyRate = vehicleType.ToLower() == "motor" ? 2000 : 5000;
            int hours = (int)Math.Ceiling(duration.TotalHours);
            return hours * hourlyRate;
        }

        private string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours} jam {duration.Minutes} menit";
        }
    }
} 