using System.Collections.Generic;

namespace ParkIRC.ViewModels
{
    public class PrinterManagementViewModel
    {
        public string CurrentPrinter { get; set; } = string.Empty;
        public List<string> AvailablePrinters { get; set; } = new List<string>();
        public bool IsPrinterConnected { get; set; }
        public string LastError { get; set; } = string.Empty;
        public PrinterSettings Settings { get; set; } = new PrinterSettings();
    }

    public class PrinterSettings
    {
        public int PaperWidth { get; set; } = 80;
        public int CharactersPerLine { get; set; } = 42;
        public bool AutoCut { get; set; } = true;
        public bool CashDrawerEnabled { get; set; } = true;
        public int PulseOn { get; set; } = 120;
        public int PulseOff { get; set; } = 240;
    }
} 