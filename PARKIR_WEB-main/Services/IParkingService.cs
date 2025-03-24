using ParkIRC.Models;
using System.Threading.Tasks;

namespace ParkIRC.Services
{
    public interface IParkingService
    {
        Task<ParkingSpace> AssignParkingSpaceAsync(Vehicle vehicle);
        Task<ParkingTransaction> ProcessExitAsync(string vehicleNumber, string paymentMethod);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<ParkingTransaction> ProcessCheckout(Vehicle vehicle);
        Task<ParkingSpace> AssignParkingSpace(Vehicle vehicle);
        Task<decimal> CalculateFee(Vehicle vehicle);
    }
} 