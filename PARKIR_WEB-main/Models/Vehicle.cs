using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class Vehicle
    {
        public Vehicle()
        {
            // Initialize collections
            Transactions = new List<ParkingTransaction>();
            
            // Initialize required string properties
            VehicleNumber = string.Empty;
            VehicleType = string.Empty;
        }
        
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Nomor kendaraan wajib diisi")]
        public string VehicleNumber { get; set; }
        
        [Required(ErrorMessage = "Tipe kendaraan wajib diisi")]
        public string VehicleType { get; set; }
        
        public string? DriverName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactNumber { get; set; }
        
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public bool IsParked { get; set; }
        
        public string? EntryPhotoPath { get; set; }
        public string? ExitPhotoPath { get; set; }
        public string? BarcodeImagePath { get; set; }
        
        public int? ParkingSpaceId { get; set; }
        
        [ForeignKey("ParkingSpaceId")]
        public virtual ParkingSpace? ParkingSpace { get; set; }
        
        public virtual ICollection<ParkingTransaction> Transactions { get; set; }
        
        public int? ShiftId { get; set; }
        
        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
    }
}