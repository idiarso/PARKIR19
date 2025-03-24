using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.Models
{
    public class EntryGate
    {
        public EntryGate()
        {
            Id = string.Empty;
            Name = string.Empty;
            Location = string.Empty;
            IpAddress = string.Empty;
        }

        [Key]
        public string Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        
        [MaxLength(255)]
        public string Location { get; set; }
        
        public bool IsOnline { get; set; }
        
        public bool IsOpen { get; set; }
        
        public DateTime? LastActivity { get; set; }
        
        [MaxLength(100)]
        public string IpAddress { get; set; }
        
        public bool HasCamera { get; set; }
        
        public bool HasPushButton { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
} 