using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class ParkingTicket
    {
        public int Id { get; set; }
        
        [Required]
        public string TicketNumber { get; set; } = string.Empty;
        
        [Required]
        public string BarcodeData { get; set; } = string.Empty;
        
        public string? BarcodeImagePath { get; set; }
        
        public DateTime IssueTime { get; set; }
        
        public DateTime? ScanTime { get; set; }
        
        public bool IsUsed { get; set; }
        
        public int? VehicleId { get; set; }
        
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }
        
        public string? OperatorId { get; set; }
        
        [ForeignKey("OperatorId")]
        public virtual Operator? IssuedByOperator { get; set; }
        
        public int ShiftId { get; set; }
        
        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; } = null!;
        
        public string Status { get; set; } = "Active";
    }
} 