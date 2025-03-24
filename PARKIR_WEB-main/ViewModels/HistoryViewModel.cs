using System;
using System.Collections.Generic;

namespace ParkIRC.ViewModels
{
    public class HistoryViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? VehicleNumber { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class HistoryTransactionViewModel
    {
        public string TransactionNumber { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
    }
}
