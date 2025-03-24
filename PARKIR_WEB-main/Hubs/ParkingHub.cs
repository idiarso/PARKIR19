using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Authorization;
using ParkIRC.Services;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ParkIRC.Hubs
{
    [Authorize]
    public class ParkingHub : Hub
    {
        private readonly IOfflineDataService _offlineDataService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ParkingHub> _logger;
        private readonly ConnectionStatusService _connectionStatusService;
        private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();
        private readonly IServiceScopeFactory _scopeFactory;

        public ParkingHub(
            ApplicationDbContext context,
            ILogger<ParkingHub> logger,
            IOfflineDataService offlineDataService,
            ConnectionStatusService connectionStatusService,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _logger = logger;
            _offlineDataService = offlineDataService;
            _connectionStatusService = connectionStatusService;
            _scopeFactory = scopeFactory;
        }

        public override async Task OnConnectedAsync()
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;
            
            _logger.LogInformation($"User {username} connected with connection ID: {connectionId}");
            
            // Track user connection
            lock (_userConnections)
            {
                _userConnections[username] = connectionId;
            }
            
            // Send current system status to newly connected client
            var systemStatus = await _connectionStatusService.GetSystemStatus();
            await Clients.Caller.SendAsync("ReceiveSystemStatus", systemStatus);
            
            // Send notification to all admin users
            if (Context.User.IsInRole("Admin"))
            {
                await Clients.User(username).SendAsync("ReceiveNotification", "System", "Welcome to ParkIRC Management Dashboard");
            }
            
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string username = Context.User.Identity.Name;
            
            _logger.LogInformation($"User {username} disconnected");
            
            // Remove user from tracking
            lock (_userConnections)
            {
                if (_userConnections.ContainsKey(username))
                {
                    _userConnections.Remove(username);
                }
            }
            
            return base.OnDisconnectedAsync(exception);
        }
        
        // Add method for clients to request the latest system status
        public async Task RequestSystemStatus()
        {
            try
            {
                var status = await _connectionStatusService.GetSystemStatus();
                await Clients.Caller.SendAsync("ReceiveSystemStatus", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system status to client");
                await Clients.Caller.SendAsync("ReceiveError", "Failed to retrieve system status");
            }
        }
        
        // Send notification to specific user or role
        public async Task SendNotification(string target, string message, string type = "info")
        {
            // Check if caller is admin
            if (!Context.User.IsInRole("Admin"))
            {
                await Clients.Caller.SendAsync("ReceiveError", "Unauthorized to send notifications");
                return;
            }
            
            if (target.StartsWith("role:"))
            {
                string role = target.Substring(5);
                // In a real app, you'd get all users in this role and send to them
                await Clients.All.SendAsync("ReceiveNotification", type, message);
            }
            else
            {
                // Send to specific user if they're connected
                lock (_userConnections)
                {
                    if (_userConnections.TryGetValue(target, out string connectionId))
                    {
                        Clients.Client(connectionId).SendAsync("ReceiveNotification", type, message);
                    }
                }
            }
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

        public async Task OpenExitGate(string spaceNumber)
        {
            await Clients.All.SendAsync("OpenExitGate", spaceNumber);
        }

        /// <summary>
        /// Called when push button is pressed at entry gate
        /// </summary>
        public async Task PushButtonPressed(string entryPoint)
        {
            try 
            {
                _logger.LogInformation($"Push button pressed at entry point: {entryPoint}");
                
                // Notify all clients about button press
                await Clients.All.SendAsync("EntryButtonPressed", entryPoint);
                
                // Trigger camera to capture vehicle
                await Clients.All.SendAsync("TriggerCamera", entryPoint);
                
                // Automatically print ticket after image is captured
                // This will be handled by the client listening for TriggerCamera
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing push button event");
            }
        }
        
        /// <summary>
        /// Print ticket for vehicle entry
        /// </summary>
        public async Task PrintEntryTicket(object ticketData)
        {
            try 
            {
                _logger.LogInformation($"Printing entry ticket");
                
                // Send to clients
                await Clients.All.SendAsync("PrintTicket", ticketData);
                
                // Here you would add actual printing logic or call the print service
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing entry ticket");
            }
        }
        
        /// <summary>
        /// Open entry gate
        /// </summary>
        public async Task OpenEntryGate(string entryPoint)
        {
            try
            {
                _logger.LogInformation($"Opening entry gate: {entryPoint}");
                await Clients.All.SendAsync("OpenEntryGate", entryPoint);
                
                // Here you would add code to actually open the gate
                // For example, call your hardware service
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening entry gate");
            }
        }

        /// <summary>
        /// Method for entry hardware to report gate status
        /// </summary>
        public async Task UpdateGateStatus(string gateId, bool isOpen)
        {
            await Clients.All.SendAsync("GateStatusChanged", gateId, isOpen);
        }
        
        /// <summary>
        /// Get status of all gates
        /// </summary>
        public async Task<List<object>> GetAllGateStatus()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var gates = await dbContext.EntryGates
                    .Select(g => new {
                        id = g.Id,
                        name = g.Name,
                        isOnline = g.IsOnline,
                        isOpen = g.IsOpen,
                        lastActivity = g.LastActivity
                    })
                    .ToListAsync();
                    
                return gates.Cast<object>().ToList();
            }
        }

        public async Task SaveOfflineData(string data)
        {
            await _offlineDataService.SaveData(data);
        }

        public async Task SyncOfflineData()
        {
            var offlineData = await _offlineDataService.GetPendingData();
            foreach (var data in offlineData)
            {
                // Proses sinkronisasi data
                await ProcessOfflineData(data);
            }
        }
    }
} 