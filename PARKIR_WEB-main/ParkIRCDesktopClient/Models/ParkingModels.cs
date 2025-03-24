using System;

namespace ParkIRCDesktopClient.Models
{
    public class VehicleEntryModel
    {
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
    }

    public class EntryResponse
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; }
        public string BarcodeData { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime EntryTime { get; set; }
        public string ParkingSpace { get; set; }
    }

    public class ExitModel
    {
        public string TicketNumber { get; set; }
        public string VehicleNumber { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class ExitResponse
    {
        public int TransactionId { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal ParkingFee { get; set; }
        public string PaymentMethod { get; set; }
    }
} 