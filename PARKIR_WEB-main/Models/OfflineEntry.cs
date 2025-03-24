using System;

namespace ParkIRC.Models
{
    public class OfflineEntry
    {
        public string Id { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool IsSynced { get; set; }
        public DateTime? SyncTime { get; set; }
        public string SyncError { get; set; } = string.Empty;
    }
} 