using System;
using System.Collections.Generic;
using ParkIRC.Models;

namespace ParkIRC.ViewModels
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
    }

    public class EntryGateViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public CameraSettings CameraSettings { get; set; } = new CameraSettings();
    }

    public class EntryGateActivity
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
} 