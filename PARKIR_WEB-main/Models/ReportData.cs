using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class ReportData
    {
        public string Period { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<VehicleDistribution> VehicleDistribution { get; set; } = new List<VehicleDistribution>();
    }

    public class VehicleDistribution
    {
        public string VehicleType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
} 