using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? startDate = null, DateTime? endDate = null, string? vehicleType = null)
        {
            var transactions = _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                transactions = transactions.Where(t => t.Vehicle != null && t.Vehicle.VehicleNumber.Contains(searchString));
            }

            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(vehicleType))
            {
                transactions = transactions.Where(t => t.Vehicle != null && t.Vehicle.VehicleType == vehicleType);
            }

            // Get transactions and add to view model
            var model = await transactions
                .OrderByDescending(t => t.EntryTime)
                .Select(t => new ParkingActivityViewModel
                {
                    VehicleNumber = t.Vehicle != null ? t.Vehicle.VehicleNumber : "Unknown",
                    EntryTime = t.EntryTime,
                    ExitTime = t.ExitTime != default(DateTime) ? t.ExitTime : null,
                    Duration = t.ExitTime != default(DateTime) ? 
                        (t.ExitTime - t.EntryTime).ToString(@"hh\:mm\:ss") : 
                        "In Progress",
                    Amount = t.TotalAmount,
                    Status = t.ExitTime != default(DateTime) ? "Completed" : "In Progress"
                })
                .ToListAsync();

            // Pass filter values to view
            ViewData["CurrentFilter"] = searchString;
            ViewData["StartDate"] = startDate;
            ViewData["EndDate"] = endDate;
            ViewData["VehicleType"] = vehicleType;

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        public async Task<IActionResult> Export(DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .AsQueryable();

            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime <= endDate.Value.AddDays(1));
            }

            var data = await transactions
                .OrderByDescending(t => t.EntryTime)
                .Select(t => new
                {
                    TransactionNumber = t.TransactionNumber,
                    VehicleNumber = t.Vehicle != null ? t.Vehicle.VehicleNumber : "Unknown",
                    VehicleType = t.Vehicle != null ? t.Vehicle.VehicleType : "Unknown",
                    EntryTime = t.EntryTime,
                    ExitTime = t.ExitTime != default(DateTime) ? t.ExitTime.ToString() : "In Progress",
                    Duration = t.ExitTime != default(DateTime) ?
                        (t.ExitTime - t.EntryTime).ToString(@"hh\:mm\:ss") :
                        "In Progress",
                    Amount = t.TotalAmount,
                    PaymentStatus = t.PaymentStatus,
                    PaymentMethod = t.PaymentMethod
                })
                .ToListAsync();

            // Convert to CSV
            var csv = "Transaction Number,Vehicle Number,Vehicle Type,Entry Time,Exit Time,Duration,Amount,Payment Status,Payment Method\n";
            foreach (var item in data)
            {
                csv += $"\"{item.TransactionNumber}\",\"{item.VehicleNumber}\",\"{item.VehicleType}\",\"{item.EntryTime}\",\"{item.ExitTime}\",\"{item.Duration}\",\"{item.Amount}\",\"{item.PaymentStatus}\",\"{item.PaymentMethod}\"\n";
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"parking-history-{DateTime.Now:yyyyMMdd}.csv");
        }
    }
} 