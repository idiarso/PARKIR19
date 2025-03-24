using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class BackupData
    {
        public DateTime BackupDate { get; set; }
        public DateRange DateRange { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<ParkingTransaction> Transactions { get; set; } = new();
        public List<ParkingTicket> Tickets { get; set; } = new();
    }

    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
} 