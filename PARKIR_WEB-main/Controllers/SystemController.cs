using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ParkIRC.Models;
using ParkIRC.Data;
using ParkIRC.Services;

namespace ParkIRC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PrintService _printService;

        public SystemController(
            ILogger<SystemController> logger,
            ApplicationDbContext context,
            PrintService printService)
        {
            _logger = logger;
            _context = context;
            _printService = printService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Check database
                bool dbConnected = await _context.Database.CanConnectAsync();
                
                // Check printer (simplified, actual implementation would be more complex)
                bool printerConnected = _printService != null;
                
                // Check camera (placeholder)
                bool cameraConnected = true;
                
                return Ok(new
                {
                    timestamp = DateTime.Now,
                    isConnected = dbConnected && printerConnected && cameraConnected,
                    database = dbConnected,
                    printer = printerConnected,
                    camera = cameraConnected
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system status");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
} 