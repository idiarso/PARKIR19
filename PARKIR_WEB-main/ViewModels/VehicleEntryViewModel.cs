using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.ViewModels
{
    public class VehicleEntryViewModel
    {
        [Required(ErrorMessage = "Nomor kendaraan wajib diisi")]
        [Display(Name = "Nomor Kendaraan")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tipe kendaraan wajib dipilih")]
        [Display(Name = "Tipe Kendaraan")]
        public string VehicleType { get; set; } = string.Empty;

        [Display(Name = "Nama Pengemudi")]
        public string DriverName { get; set; } = string.Empty;

        [Display(Name = "Nomor Kontak")]
        [Phone]
        public string ContactNumber { get; set; } = string.Empty;
    }
}
