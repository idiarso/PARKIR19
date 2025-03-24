using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;

namespace ParkIRC.Services
{
    public class ParkingService : IParkingService
    {
        private readonly ApplicationDbContext _context;

        public ParkingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ParkingSpace> AssignParkingSpaceAsync(Vehicle vehicle)
        {
            return await AssignParkingSpace(vehicle);
        }

        public async Task<ParkingTransaction> ProcessExitAsync(string vehicleNumber, string paymentMethod)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.ParkingSpace)
                .FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber && v.IsParked);

            if (vehicle == null)
            {
                throw new InvalidOperationException("Vehicle not found or not currently parked.");
            }

            var transaction = await ProcessCheckout(vehicle);
            transaction.PaymentMethod = paymentMethod;
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null;
        }

        public async Task<ParkingTransaction> ProcessCheckout(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            var exitTime = DateTime.UtcNow;
            var duration = exitTime - vehicle.EntryTime;
            var hours = Math.Ceiling(duration.TotalHours);
            var hourlyRate = vehicle.ParkingSpace?.HourlyRate ?? 0m;
            decimal totalAmount = (decimal)hours * hourlyRate;

            var transaction = new ParkingTransaction
            {
                VehicleId = vehicle.Id,
                ParkingSpaceId = vehicle.ParkingSpaceId ?? 0,
                TransactionNumber = GenerateTransactionNumber(),
                EntryTime = vehicle.EntryTime,
                ExitTime = exitTime,
                HourlyRate = hourlyRate,
                Amount = totalAmount,
                TotalAmount = totalAmount,
                PaymentStatus = "Paid",
                PaymentMethod = "Cash",
                PaymentTime = exitTime,
                Vehicle = vehicle,
                ParkingSpace = vehicle.ParkingSpace
            };

            if (vehicle.ParkingSpace != null)
            {
                vehicle.ParkingSpace.IsOccupied = false;
                vehicle.ParkingSpace.CurrentVehicle = null;
                vehicle.ParkingSpace.LastOccupiedTime = exitTime;
            }
            vehicle.ParkingSpace = null;
            vehicle.ParkingSpaceId = null;

            _context.ParkingTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<ParkingSpace> AssignParkingSpace(Vehicle vehicle)
        {
            var availableSpace = await _context.ParkingSpaces
                .FirstOrDefaultAsync(p => !p.IsOccupied);

            if (availableSpace == null)
            {
                throw new InvalidOperationException("No parking spaces available");
            }

            availableSpace.IsOccupied = true;
            availableSpace.CurrentVehicle = vehicle;
            vehicle.ParkingSpace = availableSpace;
            vehicle.EntryTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return availableSpace;
        }

        public async Task<decimal> CalculateFee(Vehicle vehicle)
        {
            if (vehicle == null || vehicle.EntryTime == default)
            {
                throw new ArgumentException("Invalid vehicle or entry time");
            }

            var duration = DateTime.UtcNow - vehicle.EntryTime;
            var hours = Math.Ceiling(duration.TotalHours);
            var space = await _context.ParkingSpaces.FindAsync(vehicle.ParkingSpaceId);
            
            if (space == null)
            {
                throw new InvalidOperationException("Vehicle is not assigned to a parking space");
            }

            return (decimal)hours * space.HourlyRate;
        }

        private static string GenerateTransactionNumber()
        {
            return "TRX-" + DateTime.Now.ToString("yyyyMMdd") + "-" + 
                   Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }
    }
} 