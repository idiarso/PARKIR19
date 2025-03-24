using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Models;
using ParkIRC.Data;
using System.Collections.Generic;
using ParkIRC.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class ReportingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(ApplicationDbContext context, ILogger<ReportingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Today.AddDays(-30);
                endDate ??= DateTime.Today;

                // Get transactions for the period
                var transactions = await _context.ParkingTransactions
                    .Where(t => t.EntryTime.Date >= startDate.Value.Date && 
                               t.EntryTime.Date <= endDate.Value.Date)
                    .ToListAsync();

                // Calculate totals using client-side evaluation
                var totalRevenue = transactions.Sum(t => t.TotalAmount);
                var totalTransactions = transactions.Count;
                var averageRevenue = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

                // Calculate occupancy
                var averageOccupancy = await CalculateAverageOccupancy(startDate, endDate);
                var peakHours = await CalculatePeakHours(startDate, endDate);

                var report = new
                {
                    StartDate = startDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    AverageRevenue = averageRevenue,
                    AverageOccupancy = averageOccupancy,
                    PeakHours = peakHours,
                    Transactions = transactions
                };

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return View("Error", new ParkIRC.Models.ErrorViewModel 
                { 
                    Message = "Error generating report: " + ex.Message,
                    RequestId = HttpContext.TraceIdentifier
                });
            }
        }

        private async Task<double> CalculateAverageOccupancy(DateTime? startDate, DateTime? endDate)
        {
            var totalSpaces = await _context.ParkingSpaces.CountAsync();
            if (totalSpaces == 0) return 0;

            var occupiedSpacesCount = await _context.ParkingSpaces
                .CountAsync(s => s.IsOccupied);

            return Math.Round((double)occupiedSpacesCount / totalSpaces * 100, 2);
        }

        private async Task<string> CalculatePeakHours(DateTime? startDate, DateTime? endDate)
        {
            var transactions = await _context.ParkingTransactions
                .Where(t => (!startDate.HasValue || t.EntryTime.Date >= startDate.Value.Date) &&
                           (!endDate.HasValue || t.EntryTime.Date <= endDate.Value.Date))
                .ToListAsync();

            if (!transactions.Any())
                return "No data available";

            var hourlyCount = transactions
                .GroupBy(t => t.EntryTime.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .First();

            return $"{hourlyCount.Hour:D2}:00 - {hourlyCount.Hour:D2}:59 ({hourlyCount.Count} vehicles)";
        }

        public async Task<IActionResult> DailyRevenue(DateTime? date = null)
        {
            try
            {
                var queryDate = date ?? DateTime.Today;
                var nextDay = queryDate.AddDays(1);

                // Get transactions for the day
                var transactions = await _context.ParkingTransactions
                    .Where(t => t.EntryTime.Date == queryDate.Date)
                    .ToListAsync();

                var totalRevenue = transactions.Sum(t => t.TotalAmount);
                var transactionCount = transactions.Count;
                var averageAmount = transactionCount > 0 ? totalRevenue / transactionCount : 0;

                var report = new
                {
                    Date = queryDate.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TransactionCount = transactionCount,
                    AverageAmount = averageAmount,
                    Transactions = transactions
                };

                return View(report);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = "Error generating daily revenue report: " + ex.Message });
            }
        }

        public async Task<IActionResult> OccupancyReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today;

            var dailyOccupancy = await _context.ParkingSpaces
                .GroupBy(s => s.LastOccupiedTime.HasValue && s.LastOccupiedTime.Value.Date >= start && s.LastOccupiedTime.Value.Date <= end 
                    ? s.LastOccupiedTime.Value.Date 
                    : DateTime.Today)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    OccupancyRate = g.Count(s => s.IsOccupied) * 100.0 / g.Count()
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return View(dailyOccupancy);
        }

        public async Task<IActionResult> VehicleTypeStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Today.AddDays(-30);
                endDate ??= DateTime.Today;

                // Get all transactions for the period
                var transactions = await _context.ParkingTransactions
                    .Include(t => t.Vehicle)
                    .Where(t => t.EntryTime.Date >= startDate.Value.Date && 
                               t.EntryTime.Date <= endDate.Value.Date &&
                               t.Vehicle != null)
                    .ToListAsync();

                // Group and calculate statistics on the client side
                var statistics = transactions
                    .GroupBy(t => t.Vehicle?.VehicleType ?? "Unknown")
                    .Select(g => new
                    {
                        VehicleType = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(t => t.TotalAmount),
                        AverageRevenue = g.Average(t => t.TotalAmount)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vehicle type statistics");
                return View("Error", new ErrorViewModel { Message = "Error generating statistics" });
            }
        }

        public async Task<IActionResult> PeakHourAnalysis(DateTime? date = null)
        {
            try
            {
                var queryDate = date ?? DateTime.Today;

                // Get all transactions for the day
                var transactions = await _context.ParkingTransactions
                    .Where(t => t.EntryTime.Date == queryDate.Date)
                    .ToListAsync();

                // Group and calculate statistics on the client side
                var hourlyAnalysis = transactions
                    .GroupBy(t => t.EntryTime.Hour)
                    .Select(g => new
                    {
                        Hour = $"{g.Key:D2}:00",
                        Count = g.Count(),
                        Revenue = g.Sum(t => t.TotalAmount),
                        AverageRevenue = g.Count() > 0 ? g.Average(t => t.TotalAmount) : 0m
                    })
                    .OrderBy(x => x.Hour)
                    .ToList();

                // Fill in missing hours with zero values
                var fullHourlyData = Enumerable.Range(0, 24)
                    .Select(hour => hourlyAnalysis.FirstOrDefault(h => h.Hour == $"{hour:D2}:00") ?? new
                    {
                        Hour = $"{hour:D2}:00",
                        Count = 0,
                        Revenue = 0m,
                        AverageRevenue = 0m
                    })
                    .ToList();

                var viewModel = new
                {
                    Date = queryDate.ToString("yyyy-MM-dd"),
                    HourlyData = fullHourlyData,
                    TotalRevenue = transactions.Sum(t => t.TotalAmount),
                    TotalTransactions = transactions.Count,
                    PeakHour = fullHourlyData.OrderByDescending(h => h.Count).First()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = "Error generating peak hour analysis: " + ex.Message });
            }
        }
    }
}