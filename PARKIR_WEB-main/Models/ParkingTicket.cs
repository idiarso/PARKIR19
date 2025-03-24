using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class ParkingTicket
    {
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public string TicketNumber { get; set; } = string.Empty;
        
        [Required]
        public string BarcodeData { get; set; } = string.Empty;
        
        public string QRCodeImage { get; set; } = string.Empty;
        
        public string? BarcodeImagePath { get; set; }
        
        public DateTime IssueTime { get; set; }
        
        public DateTime EntryTime { get; set; }
        
        public DateTime? ScanTime { get; set; }
        
        public bool IsUsed { get; set; }
        
        public int? VehicleId { get; set; }
        
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }
        
        [NotMapped]
        public string VehicleNumber { get; set; } = string.Empty;
        
        [NotMapped]
        public string VehicleType { get; set; } = string.Empty;
        
        [NotMapped]
        public string ParkingSpaceNumber { get; set; } = string.Empty;
        
        public string? OperatorId { get; set; }
        
        [ForeignKey("OperatorId")]
        public virtual Operator? IssuedByOperator { get; set; }
        
        public int ShiftId { get; set; }
        
        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; } = null!;
        
        public string Status { get; set; } = "Active";
        
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsValid { get; set; } = true;
        
        public DateTime? UsedAt { get; set; }
    }
} 