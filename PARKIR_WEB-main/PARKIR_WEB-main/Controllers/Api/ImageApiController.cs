using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkIRC.Data;
using ParkIRC.Models;

namespace ParkIRC.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ImageApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageApiController> _logger;

        public ImageApiController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ImageApiController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadModel model)
        {
            try
            {
                if (model.Image == null || model.Image.Length == 0)
                {
                    return BadRequest(new { success = false, errorMessage = "No image file was provided" });
                }

                // Validate vehicle number
                if (string.IsNullOrEmpty(model.VehicleNumber))
                {
                    return BadRequest(new { success = false, errorMessage = "Vehicle number is required" });
                }

                // Create upload directory if it doesn't exist
                string uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "vehicles");
                if (!Directory.Exists(uploadDirectory))
                {
                    Directory.CreateDirectory(uploadDirectory);
                }

                // Generate unique filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{model.TransactionType}_{model.VehicleNumber.Replace(" ", "_")}_{timestamp}.jpg";
                string filePath = Path.Combine(uploadDirectory, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                // Get relative url
                string relativePath = $"/uploads/vehicles/{fileName}";

                // Find the vehicle
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleNumber == model.VehicleNumber);

                if (vehicle != null)
                {
                    // Update vehicle with image path based on transaction type
                    if (model.TransactionType.ToLower() == "entry")
                    {
                        vehicle.EntryImagePath = relativePath;
                    }
                    else if (model.TransactionType.ToLower() == "exit")
                    {
                        vehicle.ExitImagePath = relativePath;
                    }

                    _context.Vehicles.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning($"Vehicle not found: {model.VehicleNumber}");
                }

                return Ok(new { success = true, imageUrl = relativePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { success = false, errorMessage = "Error processing image upload" });
            }
        }

        [HttpGet("{vehicleNumber}/{transactionType}")]
        public async Task<IActionResult> GetVehicleImage(string vehicleNumber, string transactionType)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber);

                if (vehicle == null)
                {
                    return NotFound(new { success = false, errorMessage = "Vehicle not found" });
                }

                string imagePath = null;
                if (transactionType.ToLower() == "entry")
                {
                    imagePath = vehicle.EntryImagePath;
                }
                else if (transactionType.ToLower() == "exit")
                {
                    imagePath = vehicle.ExitImagePath;
                }

                if (string.IsNullOrEmpty(imagePath))
                {
                    return NotFound(new { success = false, errorMessage = "Image not found for this vehicle" });
                }

                return Ok(new { success = true, imageUrl = imagePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle image");
                return StatusCode(500, new { success = false, errorMessage = "Error retrieving vehicle image" });
            }
        }
    }

    public class ImageUploadModel
    {
        public IFormFile Image { get; set; }
        public string VehicleNumber { get; set; }
        public string TransactionType { get; set; } // "entry" or "exit"
    }
} 