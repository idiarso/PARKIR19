using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Models;
using ParkIRC.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ParkIRC.Hubs;
using ParkIRC.Services;
using Microsoft.AspNetCore.Authorization;

namespace ParkIRC.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ParkingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ParkingApiController> _logger;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly IParkingService _parkingService;
        private readonly PrintService _printService;

        public ParkingApiController(
            ApplicationDbContext context,
            ILogger<ParkingApiController> logger,
            IParkingService parkingService,
            IHubContext<ParkingHub> hubContext,
            PrintService printService)
        {
            _context = context;
            _logger = logger;
            _parkingService = parkingService;
            _hubContext = hubContext;
            _printService = printService;
        }
        
        // GET: api/v1/parking/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardData>> GetDashboardData()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var totalSpaces = await _context.ParkingSpaces.CountAsync();
                var availableSpaces = await _context.ParkingSpaces.CountAsync(s => !s.IsOccupied);
                
                var dailyRevenue = await _context.ParkingTransactions
                    .Where(t => t.PaymentTime.Date == today)
                    .Select(t => t.TotalAmount)
                    .SumAsync();
                    
                var weeklyRevenue = await _context.ParkingTransactions
                    .Where(t => t.PaymentTime.Date >= weekStart && t.PaymentTime.Date <= today)
                    .Select(t => t.TotalAmount)
                    .SumAsync();
                    
                var monthlyRevenue = await _context.ParkingTransactions
                    .Where(t => t.PaymentTime.Date >= monthStart && t.PaymentTime.Date <= today)
                    .Select(t => t.TotalAmount)
                    .SumAsync();
                
                var recentActivity = await GetRecentActivity();
                var vehicleDistribution = await GetVehicleTypeDistribution();

                var dashboardData = new DashboardData
                {
                    TotalSpaces = totalSpaces,
                    AvailableSpaces = availableSpaces,
                    OccupiedSpaces = totalSpaces - availableSpaces,
                    DailyRevenue = dailyRevenue,
                    WeeklyRevenue = weeklyRevenue,
                    MonthlyRevenue = monthlyRevenue,
                    RecentActivity = recentActivity,
                    VehicleDistribution = vehicleDistribution
                };
                
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new { error = "Error retrieving dashboard data" });
            }
        }

        private async Task<List<ActivityData>> GetRecentActivity()
        {
            return await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .OrderByDescending(t => t.EntryTime)
                .Take(10)
                .Select(t => new ActivityData
                {
                    Id = t.Id,
                    VehicleNumber = t.Vehicle.VehicleNumber,
                    VehicleType = t.Vehicle.VehicleType,
                    EntryTime = t.EntryTime,
                    ExitTime = t.ExitTime,
                    TotalAmount = t.TotalAmount
                })
                .ToListAsync();
        }

        private async Task<List<VehicleTypeData>> GetVehicleTypeDistribution()
        {
            return await _context.Vehicles
                .Where(v => v.IsParked)
                .GroupBy(v => v.VehicleType)
                .Select(g => new VehicleTypeData
                {
                    VehicleType = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        // GET: api/v1/parking/vehicles
        [HttpGet("vehicles")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.IsParked)
                    .ToListAsync();
                
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parked vehicles");
                return StatusCode(500, new { error = "Error retrieving parked vehicles" });
            }
        }

        // GET: api/v1/parking/spaces
        [HttpGet("spaces")]
        public async Task<ActionResult<IEnumerable<ParkingSpace>>> GetParkingSpaces()
        {
            try
            {
                var spaces = await _context.ParkingSpaces
                    .ToListAsync();
                
                return Ok(spaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking spaces");
                return StatusCode(500, new { error = "Error retrieving parking spaces" });
            }
        }

        // POST: api/v1/parking/entry
        [HttpPost("entry")]
        public async Task<ActionResult<EntryResponse>> RecordVehicleEntry([FromBody] VehicleEntryModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validate license plate format
                if (string.IsNullOrEmpty(model.VehicleNumber))
                {
                    return BadRequest(new { error = "Vehicle license plate is required" });
                }

                // Create or retrieve the vehicle
                var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleNumber == model.VehicleNumber);
                
                if (vehicle != null && vehicle.IsParked)
                {
                    return BadRequest(new { error = "Vehicle is already parked" });
                }

                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        VehicleNumber = model.VehicleNumber,
                        VehicleType = model.VehicleType,
                        EntryTime = DateTime.Now,
                        IsParked = true
                    };
                    
                    _context.Vehicles.Add(vehicle);
                }
                else
                {
                    vehicle.EntryTime = DateTime.Now;
                    vehicle.IsParked = true;
                    vehicle.VehicleType = model.VehicleType;
                    _context.Vehicles.Update(vehicle);
                }

                // Find available parking space
                var parkingSpace = await _context.ParkingSpaces
                    .FirstOrDefaultAsync(s => !s.IsOccupied);
                
                if (parkingSpace == null)
                {
                    return BadRequest(new { error = "No available parking spaces" });
                }

                // Allocate parking space
                parkingSpace.IsOccupied = true;
                parkingSpace.CurrentVehicleId = vehicle.Id;
                _context.ParkingSpaces.Update(parkingSpace);

                // Create parking ticket
                var ticket = new ParkingTicket
                {
                    TicketNumber = $"PK-{DateTime.Now:yyMMddHHmmss}",
                    BarcodeData = $"PK-{vehicle.VehicleNumber}-{DateTime.Now:yyMMddHHmmss}",
                    IssueTime = DateTime.Now,
                    VehicleId = vehicle.Id,
                    OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                
                _context.ParkingTickets.Add(ticket);

                // Create parking transaction
                var transaction = new ParkingTransaction
                {
                    TransactionNumber = $"TX-{DateTime.Now:yyMMddHHmmss}",
                    VehicleId = vehicle.Id,
                    ParkingSpaceId = parkingSpace.Id,
                    EntryTime = DateTime.Now,
                    OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HourlyRate = parkingSpace.HourlyRate
                };
                
                _context.ParkingTransactions.Add(transaction);
                
                await _context.SaveChangesAsync();

                // Notify clients via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveParkingUpdate", new { 
                    type = "entry", 
                    vehicle = vehicle.VehicleNumber,
                    space = parkingSpace.SpaceNumber
                });

                return Ok(new EntryResponse
                {
                    TicketId = ticket.Id,
                    TicketNumber = ticket.TicketNumber,
                    BarcodeData = ticket.BarcodeData,
                    VehicleNumber = vehicle.VehicleNumber,
                    EntryTime = vehicle.EntryTime,
                    ParkingSpace = parkingSpace.SpaceNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle entry");
                return StatusCode(500, new { error = "Error processing vehicle entry" });
            }
        }

        // POST: api/v1/parking/exit
        [HttpPost("exit")]
        public async Task<ActionResult<ExitResponse>> RecordVehicleExit([FromBody] ExitModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Find vehicle by ticket number or license plate
                Vehicle vehicle;
                ParkingTransaction transaction;
                
                if (!string.IsNullOrEmpty(model.TicketNumber))
                {
                    var ticket = await _context.ParkingTickets
                        .FirstOrDefaultAsync(t => t.TicketNumber == model.TicketNumber);
                    
                    if (ticket == null)
                    {
                        return NotFound(new { error = "Ticket not found" });
                    }
                    
                    vehicle = await _context.Vehicles.FindAsync(ticket.VehicleId);
                }
                else if (!string.IsNullOrEmpty(model.VehicleNumber))
                {
                    vehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.VehicleNumber == model.VehicleNumber && v.IsParked);
                }
                else
                {
                    return BadRequest(new { error = "Ticket number or vehicle license plate is required" });
                }
                
                if (vehicle == null || !vehicle.IsParked)
                {
                    return NotFound(new { error = "Vehicle not found in parking" });
                }

                // Find the associated transaction
                transaction = await _context.ParkingTransactions
                    .Include(t => t.ParkingSpace)
                    .FirstOrDefaultAsync(t => t.VehicleId == vehicle.Id && t.ExitTime == default);
                
                if (transaction == null)
                {
                    return NotFound(new { error = "No active parking transaction found for this vehicle" });
                }

                // Calculate parking duration and fee
                var exitTime = DateTime.Now;
                var duration = exitTime - transaction.EntryTime;
                var hours = Math.Ceiling(duration.TotalHours);
                var parkingFee = hours * transaction.HourlyRate;

                // Update transaction
                transaction.ExitTime = exitTime;
                transaction.Duration = duration;
                transaction.TotalAmount = parkingFee;
                transaction.PaymentTime = exitTime;
                transaction.PaymentMethod = model.PaymentMethod;
                
                _context.ParkingTransactions.Update(transaction);

                // Update vehicle and parking space
                vehicle.IsParked = false;
                var parkingSpace = transaction.ParkingSpace;
                parkingSpace.IsOccupied = false;
                parkingSpace.CurrentVehicleId = null;
                
                _context.Vehicles.Update(vehicle);
                _context.ParkingSpaces.Update(parkingSpace);
                
                await _context.SaveChangesAsync();

                // Notify clients via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveParkingUpdate", new { 
                    type = "exit", 
                    vehicle = vehicle.VehicleNumber,
                    space = parkingSpace.SpaceNumber
                });

                return Ok(new ExitResponse
                {
                    TransactionId = transaction.Id,
                    VehicleNumber = vehicle.VehicleNumber,
                    EntryTime = transaction.EntryTime,
                    ExitTime = exitTime,
                    Duration = duration,
                    ParkingFee = parkingFee,
                    PaymentMethod = model.PaymentMethod
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle exit");
                return StatusCode(500, new { error = "Error processing vehicle exit" });
            }
        }
    }

    public class DashboardData
    {
        public int TotalSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<ActivityData> RecentActivity { get; set; }
        public List<VehicleTypeData> VehicleDistribution { get; set; }
    }

    public class ActivityData
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class VehicleTypeData
    {
        public string VehicleType { get; set; }
        public int Count { get; set; }
    }

    public class EntryResponse
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; }
        public string BarcodeData { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime EntryTime { get; set; }
        public string ParkingSpace { get; set; }
    }

    public class ExitModel
    {
        public string TicketNumber { get; set; }
        public string VehicleNumber { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class ExitResponse
    {
        public int TransactionId { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal ParkingFee { get; set; }
        public string PaymentMethod { get; set; }
    }
} 