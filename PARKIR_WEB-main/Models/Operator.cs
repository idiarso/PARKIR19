using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ParkIRC.Models
{
    public class Operator : IdentityUser
    {
        public Operator()
        {
            Shifts = new List<Shift>();
        }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public override string? Email { get; set; }

        [Phone]
        public override string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedAt { get; set; }

        public string? LastModifiedBy { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? BadgeNumber { get; set; }
        
        public DateTime JoinDate { get; set; }
        
        public string? PhotoPath { get; set; }
        
        public virtual ICollection<Shift> Shifts { get; set; }
    }
} 