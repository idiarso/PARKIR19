using System;
using System.Collections.Generic;

namespace ParkIRCDesktopClient.Models
{
    public class DashboardData
    {
        public int TotalSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<ActivityData> RecentActivity { get; set; }
        public List<VehicleTypeData> VehicleDistribution { get; set; }
    }

    public class ActivityData
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class VehicleTypeData
    {
        public string VehicleType { get; set; }
        public int Count { get; set; }
    }
} 