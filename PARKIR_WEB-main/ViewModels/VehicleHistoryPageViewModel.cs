using System;
using System.Collections.Generic;

namespace ParkIRC.ViewModels
{
    public class VehicleHistoryPageViewModel
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public List<VehicleHistoryPageItemViewModel> Transactions { get; set; } = new List<VehicleHistoryPageItemViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class VehicleHistoryPageItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public TimeSpan Duration => ExitTime.HasValue ? ExitTime.Value - EntryTime : TimeSpan.Zero;
        public string Status => ExitTime.HasValue ? "Keluar" : "Masuk";
        public decimal TotalAmount { get; set; }
    }
} 