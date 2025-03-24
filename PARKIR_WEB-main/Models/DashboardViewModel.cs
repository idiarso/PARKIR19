using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class DashboardViewModel
    {
        public int TotalSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<ParkingActivity> RecentActivity { get; set; } = new();
        public List<OccupancyData> HourlyOccupancy { get; set; } = new();
        public List<VehicleDistributionData> VehicleDistribution { get; set; } = new();
    }

    public class OccupancyData
    {
        public string Hour { get; set; } = string.Empty;
        public int Count { get; set; }
        public double OccupancyPercentage { get; set; }
    }

    public class OccupancyDataComparer : IEqualityComparer<OccupancyData>
    {
        public bool Equals(OccupancyData? x, OccupancyData? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Hour == y.Hour;
        }

        public int GetHashCode(OccupancyData obj)
        {
            return obj?.Hour.GetHashCode() ?? 0;
        }
    }

    public class VehicleDistributionData
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ParkingActivity
    {
        public string VehicleType { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string ParkingType { get; set; } = string.Empty;
    }
} 