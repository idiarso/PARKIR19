using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class Journal
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Action { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string OperatorId { get; set; } = string.Empty;
        
        [ForeignKey("OperatorId")]
        public virtual Operator? Operator { get; set; }
    }
} 