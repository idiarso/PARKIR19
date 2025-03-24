using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.ViewModels
{
    public class VehicleExitViewModel
    {
        [Required(ErrorMessage = "Nomor kendaraan wajib diisi")]
        [Display(Name = "Nomor Kendaraan")]
        public string VehicleNumber { get; set; }

        [Display(Name = "Metode Pembayaran")]
        public string PaymentMethod { get; set; } = "Cash";

        [Display(Name = "Nominal Pembayaran")]
        public decimal? PaymentAmount { get; set; }

        public string? TransactionNumber { get; set; }
    }
}
