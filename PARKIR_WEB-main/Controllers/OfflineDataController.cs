using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using ParkIRC.Models;
using ParkIRC.Data;
using ParkIRC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfflineDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OfflineDataController> _logger;

        public OfflineDataController(ApplicationDbContext context, ILogger<OfflineDataController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        [HttpPost("sync")]
        public async Task<IActionResult> SyncOfflineData([FromBody] List<OfflineEntry> entries)
        {
            try
            {
                foreach (var entry in entries)
                {
                    // Validate duplicate entries
                    if (!await _context.ParkingTransactions.AnyAsync(t => t.TicketNumber == entry.TicketNumber))
                    {
                        var transaction = new ParkingTransaction
                        {
                            TicketNumber = entry.TicketNumber,
                            VehicleNumber = entry.VehicleNumber,
                            VehicleType = entry.VehicleType,
                            EntryTime = entry.Timestamp,
                            EntryPoint = entry.DeviceId,
                            IsOfflineEntry = true,
                            ImagePath = entry.ImagePath,
                            OperatorId = entry.OperatorId,
                            Status = "Active",
                            PaymentStatus = "Pending",
                            PaymentMethod = "Cash"
                        };
                        
                        _context.ParkingTransactions.Add(transaction);
                    }
                }
                
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data synced successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing offline data");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 