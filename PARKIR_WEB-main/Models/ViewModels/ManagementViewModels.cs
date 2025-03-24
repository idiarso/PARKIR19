using System.Collections.Generic;

namespace ParkIRC.Models.ViewModels
{
    public class BackupViewModel
    {
        public List<string> BackupOptions { get; set; } = new List<string>();
        public List<string> SelectedOptions { get; set; } = new List<string>();
        public List<string> AvailableBackups { get; set; } = new List<string>();
    }
    
    public class RestoreViewModel
    {
        public List<string> AvailableBackups { get; set; } = new List<string>();
        public string SelectedBackup { get; set; } = string.Empty;
        public bool RestoreDatabase { get; set; } = true;
        public bool RestoreUploads { get; set; } = true;
        public bool RestoreConfig { get; set; } = true;
    }
    
    public class PrinterManagementViewModel
    {
        public List<string> AvailablePrinters { get; set; } = new List<string>();
        public string CurrentPrinter { get; set; } = string.Empty;
    }
    
    public class SystemStatusViewModel
    {
        public bool DatabaseStatus { get; set; }
        public bool PostgresServiceStatus { get; set; }
        public bool CupsServiceStatus { get; set; }
        
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
} 