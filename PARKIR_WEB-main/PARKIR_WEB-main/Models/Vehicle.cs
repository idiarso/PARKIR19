using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "License Plate")]
        public string VehicleNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; }

        [Display(Name = "Entry Time")]
        public DateTime EntryTime { get; set; }

        [Display(Name = "Exit Time")]
        public DateTime? ExitTime { get; set; }

        [Display(Name = "Is Parked")]
        public bool IsParked { get; set; }

        // Lokasi gambar entry
        [StringLength(500)]
        public string EntryImagePath { get; set; }

        // Lokasi gambar exit
        [StringLength(500)]
        public string ExitImagePath { get; set; }

        // Navigation property for parking space
        public ParkingSpace ParkingSpace { get; set; }

        public int? ShiftId { get; set; }
        public Shift Shift { get; set; }

        // Navigation properties for transactions
        public ICollection<ParkingTransaction> Transactions { get; set; }

        // Navigation properties for tickets
        public ICollection<ParkingTicket> Tickets { get; set; }
    }
} 