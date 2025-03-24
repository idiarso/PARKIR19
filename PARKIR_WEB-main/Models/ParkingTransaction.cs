using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class ParkingTransaction
    {
        public ParkingTransaction()
        {
            // Initialize required string properties
            TransactionNumber = string.Empty;
            PaymentStatus = "Pending";
            PaymentMethod = string.Empty;
            Status = "Active";
            OperatorId = string.Empty;
            EntryPoint = string.Empty;
            TicketNumber = string.Empty;
            VehicleId = string.Empty;
            VehicleNumber = string.Empty;
            VehicleType = string.Empty;
            ParkingSpaceId = string.Empty;
            ImagePath = string.Empty;
        }

        public string Id { get; set; } = string.Empty;

        [Required]
        public string TransactionNumber { get; set; }

        [Required]
        public string TicketNumber { get; set; }

        [Required]
        public string VehicleId { get; set; }

        public string ParkingSpaceId { get; set; }

        [Required]
        public DateTime EntryTime { get; set; }

        public DateTime? ExitTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Required]
        public string PaymentStatus { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public DateTime? PaymentTime { get; set; }

        [Required]
        public string Status { get; set; }

        public string OperatorId { get; set; }

        public string EntryPoint { get; set; }

        public string VehicleNumber { get; set; }

        public string VehicleType { get; set; }

        public string ImagePath { get; set; }

        public bool IsOfflineEntry { get; set; }

        public bool IsManualEntry { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey("ParkingSpaceId")]
        public virtual ParkingSpace? ParkingSpace { get; set; }

        [NotMapped]
        public TimeSpan Duration => ExitTime.HasValue ? ExitTime.Value - EntryTime : TimeSpan.Zero;
    }
}