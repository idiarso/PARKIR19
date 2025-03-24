using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Hubs
{
    public class ParkingHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ParkingHub> _logger;

        public ParkingHub(ApplicationDbContext context, ILogger<ParkingHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Called by clients to update dashboard data for all clients
        /// </summary>
        /// <param name="data">Dashboard data to be broadcasted</param>
        public async Task UpdateDashboard()
        {
            try
            {
                var today = DateTime.Today;
                var data = new
                {
                    totalSpaces = await _context.ParkingSpaces.CountAsync(),
                    availableSpaces = await _context.ParkingSpaces.CountAsync(x => !x.IsOccupied),
                    occupiedSpaces = await _context.ParkingSpaces.CountAsync(x => x.IsOccupied),
                    todayRevenue = await _context.ParkingTransactions
                        .Where(x => x.EntryTime.Date == today)
                        .SumAsync(x => x.TotalAmount),
                    vehicleDistribution = await _context.Vehicles
                        .Where(x => x.IsParked)
                        .GroupBy(x => x.VehicleType)
                        .Select(g => new { type = g.Key, count = g.Count() })
                        .ToListAsync(),
                    recentActivities = await _context.ParkingTransactions
                        .Include(x => x.Vehicle)
                        .OrderByDescending(x => x.EntryTime)
                        .Take(10)
                        .Select(x => new
                        {
                            time = x.EntryTime.ToString("HH:mm"),
                            vehicleNumber = x.Vehicle.VehicleNumber,
                            vehicleType = x.Vehicle.VehicleType,
                            status = x.ExitTime != default(DateTime) ? "Exit" : "Entry",
                            totalItems = _context.ParkingTransactions.Count(),
                            currentPage = 1,
                            pageSize = 10
                        })
                        .ToListAsync()
                };

                await Clients.All.SendAsync("UpdateDashboard", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dashboard");
            }
        }

        /// <summary>
        /// Called by clients to notify about new vehicle entry
        /// </summary>
        /// <param name="vehicleNumber">The vehicle number that entered</param>
        public async Task NotifyVehicleEntry(string vehicleNumber)
        {
            await Clients.All.SendAsync("VehicleEntryNotification", vehicleNumber);
        }

        /// <summary>
        /// Called by clients to notify about vehicle exit
        /// </summary>
        /// <param name="vehicleNumber">The vehicle number that exited</param>
        public async Task NotifyVehicleExit(string vehicleNumber)
        {
            await Clients.All.SendAsync("VehicleExitNotification", vehicleNumber);
        }
        
        /// <summary>
        /// Called by ANPR system to broadcast plate detection result
        /// </summary>
        /// <param name="licensePlate">The detected license plate number</param>
        /// <param name="isSuccessful">Whether the detection was successful</param>
        public async Task NotifyPlateDetection(string licensePlate, bool isSuccessful)
        {
            await Clients.All.SendAsync("PlateDetectionResult", new { 
                licensePlate, 
                isSuccessful 
            });
        }
        
        /// <summary>
        /// Called by systems to notify when barrier opens or closes
        /// </summary>
        /// <param name="isEntry">True if entry barrier, false if exit barrier</param>
        /// <param name="isOpen">True if opened, false if closed</param>
        public async Task NotifyBarrierStatus(bool isEntry, bool isOpen)
        {
            await Clients.All.SendAsync("BarrierStatusChanged", new { 
                isEntry, 
                isOpen,
                barrierType = isEntry ? "Entry" : "Exit",
                status = isOpen ? "Open" : "Closed"
            });
        }

        public async Task GetPagedActivities(int pageNumber)
        {
            try
            {
                var pageSize = 10;
                var query = _context.ParkingTransactions
                    .Include(x => x.Vehicle)
                    .OrderByDescending(x => x.EntryTime);

                var totalItems = await query.CountAsync();
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        time = x.EntryTime.ToString("HH:mm"),
                        vehicleNumber = x.Vehicle.VehicleNumber,
                        vehicleType = x.Vehicle.VehicleType,
                        status = x.ExitTime != default(DateTime) ? "Exit" : "Entry"
                    })
                    .ToListAsync();

                var data = new
                {
                    items,
                    totalItems,
                    currentPage = pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                await Clients.Caller.SendAsync("UpdatePagedActivities", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged activities");
            }
        }
    }
} 