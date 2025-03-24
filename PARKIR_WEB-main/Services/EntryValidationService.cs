using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkIRC.Data;
using ParkIRC.Models;

namespace ParkIRC.Services
{
    public class EntryValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EntryValidationService> _logger;

        public EntryValidationService(ApplicationDbContext context, ILogger<EntryValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateEntry(string vehicleNumber, string operatorId)
        {
            try
            {
                // Validasi format plat
                if (!IsValidPlateFormat(vehicleNumber))
                {
                    return new ValidationResult 
                    { 
                        IsValid = false,
                        Message = "Format plat nomor tidak valid"
                    };
                }

                // Cek duplikasi (kendaraan masih di dalam)
                var existingVehicle = await _context.ParkingTransactions
                    .Where(t => t.VehicleNumber == vehicleNumber && t.ExitTime == null)
                    .FirstOrDefaultAsync();

                if (existingVehicle != null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Kendaraan masih berada di dalam area parkir"
                    };
                }

                // Log entry
                await _context.Journals.AddAsync(new Journal
                {
                    Action = "VehicleEntry",
                    Description = $"Vehicle {vehicleNumber} entered",
                    Timestamp = DateTime.Now,
                    OperatorId = operatorId
                });

                await _context.SaveChangesAsync();

                return new ValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating entry");
                throw;
            }
        }

        private bool IsValidPlateFormat(string plateNumber)
        {
            // Format: 1-2 huruf, spasi, 1-4 angka, spasi, 1-3 huruf
            var regex = new Regex(@"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$");
            return regex.IsMatch(plateNumber);
        }
    }
} 