using System;
using System.Collections.Generic;

namespace ParkIRC.ViewModels
{
    public class BackupViewModel
    {
        public List<string> BackupOptions { get; set; } = new();
        public List<string> SelectedOptions { get; set; } = new();
        public List<string> AvailableBackups { get; set; } = new();
        public string BackupName { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public long BackupSize { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RestoreViewModel
    {
        public List<string> AvailableBackups { get; set; } = new();
        public string SelectedBackup { get; set; } = string.Empty;
        public bool RestoreDatabase { get; set; }
        public bool RestoreUploads { get; set; }
        public bool RestoreConfig { get; set; }
        public bool IncludeData { get; set; }
        public bool IncludeSettings { get; set; }
        public bool BackupCurrentState { get; set; }
    }

    public class SystemStatusViewModel
    {
        public bool DatabaseConnected { get; set; }
        public bool PrinterConnected { get; set; }
        public bool CameraConnected { get; set; }
        public string LastBackupDate { get; set; } = string.Empty;
        public string DiskSpace { get; set; } = string.Empty;
        public List<string> ActiveUsers { get; set; } = new();
        public List<SystemAlert> Alerts { get; set; } = new();
        public string DatabaseStatus { get; set; } = string.Empty;
        public string PostgresServiceStatus { get; set; } = string.Empty;
        public string CupsServiceStatus { get; set; } = string.Empty;
        public string DiskTotal { get; set; } = string.Empty;
        public string DiskUsed { get; set; } = string.Empty;
        public string DiskFree { get; set; } = string.Empty;
        public string DiskUsagePercent { get; set; } = string.Empty;
        public string MemoryTotal { get; set; } = string.Empty;
        public string MemoryUsed { get; set; } = string.Empty;
        public string MemoryFree { get; set; } = string.Empty;
        public string SystemUptime { get; set; } = string.Empty;
        public string LoadAverage1 { get; set; } = string.Empty;
        public string LoadAverage5 { get; set; } = string.Empty;
        public string LoadAverage15 { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class SystemAlert
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ParkingRateViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal DailyRate { get; set; }
        public decimal WeeklyRate { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal PenaltyRate { get; set; }
        public decimal MotorcycleRate { get; set; }
        public decimal CarRate { get; set; }
        public decimal AdditionalHourRate { get; set; }
        public decimal MaximumDailyRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public bool IsActive { get; set; }
    }
} 