using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Models.ViewModels;
using ParkIRC.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParkIRC.Controllers
{
    [Authorize(Roles = "Admin,Operator")]
    public class EntryGateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EntryGateController> _logger;
        private readonly ConnectionStatusService _connectionStatusService;
        private readonly CameraService _cameraService;
        private readonly StorageService _storageService;
        private readonly TicketService _ticketService;
        private readonly PrinterService _printerService;

        public EntryGateController(
            ApplicationDbContext context,
            ILogger<EntryGateController> logger,
            ConnectionStatusService connectionStatusService,
            CameraService cameraService,
            StorageService storageService,
            TicketService ticketService,
            PrinterService printerService)
        {
            _context = context;
            _logger = logger;
            _connectionStatusService = connectionStatusService;
            _cameraService = cameraService;
            _storageService = storageService;
            _ticketService = ticketService;
            _printerService = printerService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new EntryGateMonitoringViewModel();
            
            // Get entry gates statuses
            model.EntryGates = await _context.EntryGates
                .Where(g => g.IsActive)
                .Select(g => new EntryGateStatusViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    IsOnline = g.IsOnline,
                    IsOpen = g.IsOpen,
                    LastActivity = g.LastActivity,
                    VehiclesProcessed = _context.ParkingTransactions
                        .Count(t => t.EntryPoint == g.Id && t.EntryTime.Date == DateTime.Today)
                })
                .ToListAsync();
            
            // Get recent transactions
            model.RecentTransactions = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Where(t => t.EntryTime > DateTime.Now.AddHours(-1))
                .OrderByDescending(t => t.EntryTime)
                .Take(10)
                .Select(t => new RecentTransactionViewModel
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    VehicleNumber = t.Vehicle.VehicleNumber,
                    VehicleType = t.Vehicle.VehicleType,
                    EntryTime = t.EntryTime,
                    EntryPoint = t.EntryPoint
                })
                .ToListAsync();
            
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEntryGate(EntryGate gate)
        {
            if (ModelState.IsValid)
            {
                gate.Id = $"ENTRY{await _context.EntryGates.CountAsync() + 1}";
                gate.IsActive = true;
                gate.LastActivity = DateTime.Now;
                
                _context.EntryGates.Add(gate);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"New entry gate added: {gate.Name}");
                
                return RedirectToAction(nameof(Index));
            }
            
            return View(gate);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEntryGate(string id, bool isActive)
        {
            var gate = await _context.EntryGates.FindAsync(id);
            if (gate == null)
            {
                return NotFound();
            }
            
            gate.IsActive = isActive;
            gate.LastActivity = DateTime.Now;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Entry gate {id} status updated: Active = {isActive}");
            
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetGateStatus(string id)
        {
            var gate = await _context.EntryGates.FindAsync(id);
            if (gate == null)
            {
                return NotFound();
            }
            
            return Json(new
            {
                id = gate.Id,
                name = gate.Name,
                isOnline = gate.IsOnline,
                isOpen = gate.IsOpen,
                lastActivity = gate.LastActivity
            });
        }

        [HttpPost]
        public async Task<IActionResult> CaptureEntry([FromBody] EntryRequest request)
        {
            try {
                // Capture foto kendaraan
                var imageBytes = await _cameraService.CaptureImage();
                
                // Generate nama file unik
                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{request.GateId}.jpg";
                
                // Simpan foto
                await _storageService.SaveImage(imageBytes, fileName);
                
                // Buat transaksi parkir
                var transaction = new ParkingTransaction 
                {
                    TicketNumber = GenerateTicketNumber(),
                    EntryTime = DateTime.Now,
                    EntryPoint = request.GateId,
                    VehicleImagePath = fileName,
                    VehicleNumber = request.VehicleNumber, // Input manual
                    VehicleType = request.VehicleType,     // Input manual/preset
                    IsManualEntry = true
                };

                await _context.ParkingTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    ticketNumber = transaction.TicketNumber,
                    imagePath = fileName
                });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error processing entry");
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignSpace(VehicleEntryRequest request)
        {
            try
            {
                // Automatic space assignment
                var availableSpace = await _context.ParkingSpaces
                    .Where(s => !s.IsOccupied && s.VehicleType == request.VehicleType)
                    .OrderBy(s => s.SpaceNumber)
                    .FirstOrDefaultAsync();

                if (availableSpace == null)
                {
                    return BadRequest(new { message = "Tidak ada slot parkir tersedia" });
                }

                // Generate ticket number
                string ticketNumber = await _ticketService.GenerateTicketNumber(request.VehicleType);

                // Create transaction
                var transaction = new ParkingTransaction
                {
                    TicketNumber = ticketNumber,
                    VehicleNumber = request.VehicleNumber,
                    EntryTime = DateTime.Now,
                    ParkingSpaceId = availableSpace.Id,
                    VehicleType = request.VehicleType,
                    ImagePath = request.ImagePath
                };

                _context.ParkingTransactions.Add(transaction);
                
                // Update space status
                availableSpace.IsOccupied = true;
                availableSpace.CurrentVehicle = request.VehicleNumber;

                await _context.SaveChangesAsync();

                // Print ticket
                await _printerService.PrintEntryTicket(transaction);

                return Ok(new { 
                    ticketNumber = ticketNumber,
                    spaceNumber = availableSpace.SpaceNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automatic space assignment");
                return StatusCode(500);
            }
        }
    }
} 