using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ParkIRC.Services
{
    public interface IScannerService
    {
        Task<Vehicle?> ProcessScannedCode(string scannedCode);
        Task<ParkingTicket?> GetTicketByCode(string scannedCode);
        Task<bool> ValidateScannedCode(string scannedCode);
        string GenerateBarcodeData(Vehicle vehicle);
    }

    public class ScannerService : IScannerService
    {
        private readonly ILogger<ScannerService> _logger;
        private readonly ApplicationDbContext _context;

        public ScannerService(ILogger<ScannerService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Process a scanned barcode/QR code and return the associated vehicle
        /// </summary>
        /// <param name="scannedCode">The scanned code content</param>
        /// <returns>The vehicle associated with the code, or null if not found</returns>
        public async Task<Vehicle?> ProcessScannedCode(string scannedCode)
        {
            try
            {
                _logger.LogInformation($"Processing scanned code: {scannedCode}");
                
                // Parse the code (format: vehicleNumber|entryTime)
                var parts = scannedCode.Split('|');
                if (parts.Length < 1)
                {
                    _logger.LogWarning("Invalid code format");
                    return null;
                }

                var vehicleNumber = parts[0];
                
                // Find the vehicle
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber && v.IsParked);
                
                if (vehicle == null)
                {
                    _logger.LogWarning($"Vehicle not found for code: {scannedCode}");
                    return null;
                }

                _logger.LogInformation($"Found vehicle: {vehicle.VehicleNumber}");
                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing scanned code: {scannedCode}");
                return null;
            }
        }

        /// <summary>
        /// Get a parking ticket by its barcode data
        /// </summary>
        /// <param name="scannedCode">The scanned code content</param>
        /// <returns>The parking ticket, or null if not found</returns>
        public async Task<ParkingTicket?> GetTicketByCode(string scannedCode)
        {
            try
            {
                var ticket = await _context.ParkingTickets
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.BarcodeData == scannedCode && !t.IsUsed);
                
                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting ticket by code: {scannedCode}");
                return null;
            }
        }

        /// <summary>
        /// Validate if a scanned code is valid and can be processed
        /// </summary>
        /// <param name="scannedCode">The scanned code content</param>
        /// <returns>True if the code is valid, false otherwise</returns>
        public async Task<bool> ValidateScannedCode(string scannedCode)
        {
            try
            {
                // Check if code is in the correct format
                var parts = scannedCode.Split('|');
                if (parts.Length < 1)
                {
                    return false;
                }

                var vehicleNumber = parts[0];
                
                // Check if vehicle exists and is parked
                var vehicleExists = await _context.Vehicles
                    .AnyAsync(v => v.VehicleNumber == vehicleNumber && v.IsParked);
                
                return vehicleExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating scanned code: {scannedCode}");
                return false;
            }
        }

        /// <summary>
        /// Generate barcode data for a vehicle
        /// </summary>
        /// <param name="vehicle">The vehicle</param>
        /// <returns>The barcode data string</returns>
        public string GenerateBarcodeData(Vehicle vehicle)
        {
            return $"{vehicle.VehicleNumber}|{vehicle.EntryTime:yyyyMMddHHmmss}";
        }
    }
}