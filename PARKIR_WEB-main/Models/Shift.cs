using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ParkIRC.Models
{
    public class Shift
    {
        private List<string>? _workDays;
        private string _workDaysString = string.Empty;

        public Shift()
        {
            Vehicles = new List<Vehicle>();
            Operators = new List<Operator>();
            ShiftName = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            Date = DateTime.Today;
            _workDays = new List<string>();
            _workDaysString = string.Empty;
            CreatedAt = DateTime.UtcNow;
        }

        public int Id { get; set; }
        
        [Required(ErrorMessage = "Nama shift wajib diisi")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Nama shift wajib diisi")]
        public string ShiftName { get; set; }
        
        [Required(ErrorMessage = "Tanggal wajib diisi")]
        public DateTime Date { get; set; }
        
        [Required(ErrorMessage = "Waktu mulai wajib diisi")]
        public DateTime StartTime { get; set; }
        
        [Required(ErrorMessage = "Waktu selesai wajib diisi")]
        public DateTime EndTime { get; set; }
        
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Jumlah operator maksimal wajib diisi")]
        [Range(1, 100, ErrorMessage = "Jumlah operator maksimal harus antara 1-100")]
        public int MaxOperators { get; set; }
        
        public bool IsActive { get; set; }

        [NotMapped]
        public List<string> WorkDays
        {
            get
            {
                if (_workDays == null || (_workDays.Count == 0 && !string.IsNullOrEmpty(_workDaysString)))
                {
                    _workDays = new List<string>();
                    if (!string.IsNullOrEmpty(_workDaysString))
                    {
                        _workDays.AddRange(_workDaysString.Split(',', StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                return _workDays;
            }
            set
            {
                _workDays = value ?? new List<string>();
                _workDaysString = string.Join(",", _workDays);
            }
        }

        [Column("WorkDays")]
        public string WorkDaysString
        {
            get => _workDaysString;
            set
            {
                _workDaysString = value ?? string.Empty;
                _workDays = null;
            }
        }
        
        public DateTime CreatedAt { get; set; }
        
        public virtual ICollection<Vehicle> Vehicles { get; set; }
        public virtual ICollection<Operator> Operators { get; set; }

        // Helper method to check if a given time falls within this shift
        public bool IsTimeInShift(DateTime time)
        {
            var timeOfDay = time.TimeOfDay;
            var startTimeOfDay = StartTime.TimeOfDay;
            var endTimeOfDay = EndTime.TimeOfDay;

            if (endTimeOfDay > startTimeOfDay)
            {
                // Normal shift (e.g., 9:00 to 17:00)
                return timeOfDay >= startTimeOfDay && timeOfDay <= endTimeOfDay;
            }
            else
            {
                // Overnight shift (e.g., 22:00 to 6:00)
                return timeOfDay >= startTimeOfDay || timeOfDay <= endTimeOfDay;
            }
        }
    }
} 