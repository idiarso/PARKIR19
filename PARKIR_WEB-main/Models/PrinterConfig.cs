using System;

namespace ParkIRC.Models
{
    public class PrinterConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string IpAddress { get; set; } = string.Empty;
        public int? TcpPort { get; set; }
        public string ConnectionType { get; set; } = "Serial"; // "Serial", "Network", "WebSocket"
        public string Status { get; set; } = "Offline";
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }
} 