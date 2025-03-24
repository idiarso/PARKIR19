using System;

namespace ParkIRC.ViewModels
{
    public class ReportViewModel
    {
        public string ReportType { get; set; }
        public DateTime? ReportDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ReportFilter
    {
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
