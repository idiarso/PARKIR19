using System;
using System.Collections.Generic;

namespace ParkIRC.Models.ViewModels
{
    public class EntryGateMonitoringViewModel
    {
        public string GateId { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastActivityTime { get; set; } = DateTime.Now;
        public List<EntryGateActivity> RecentActivities { get; set; } = new List<EntryGateActivity>();
        public CameraSettings CameraSettings { get; set; } = new CameraSettings();
        public List<EntryGateStatusViewModel> EntryGates { get; set; } = new List<EntryGateStatusViewModel>();
        public List<RecentTransactionViewModel> RecentTransactions { get; set; } = new List<RecentTransactionViewModel>();
    }
    
    public class EntryGateStatusViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool IsOpen { get; set; }
        public DateTime? LastActivity { get; set; }
        public int VehiclesProcessed { get; set; }
    }
    
    public class RecentTransactionViewModel
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public string EntryPoint { get; set; } = string.Empty;
    }

    public class EntryGateActivity
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CameraSettings
    {
        public string Resolution { get; set; } = string.Empty;
        public int Brightness { get; set; }
        public int Contrast { get; set; }
        public bool IsEnabled { get; set; }
    }
} 