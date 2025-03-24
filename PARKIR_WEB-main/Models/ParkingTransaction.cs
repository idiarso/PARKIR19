using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.Models
{
    public class ParkingTransaction
    {
        public ParkingTransaction()
        {
            // Initialize required string properties
            TransactionNumber = string.Empty;
            PaymentStatus = "Pending";
            PaymentMethod = "Cash";
            Status = "Active";
        }
        
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ParkingSpaceId { get; set; }
        
        [Required]
        public string TransactionNumber { get; set; }
        
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        
        [Required]
        public string PaymentStatus { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; }
        
        [Required]
        public string Status { get; set; }
        
        public DateTime PaymentTime { get; set; }
        
        public virtual Vehicle? Vehicle { get; set; }
        public virtual ParkingSpace? ParkingSpace { get; set; }
    }
}