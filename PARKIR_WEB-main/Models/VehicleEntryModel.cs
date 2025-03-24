using System.ComponentModel.DataAnnotations;

namespace ParkIRC.Models
{
    public class VehicleEntryModel
    {
        [Required(ErrorMessage = "Nomor kendaraan wajib diisi")]
        [RegularExpression(@"^[A-Z]{1,2}\s\d{1,4}\s[A-Z]{1,3}$", 
            ErrorMessage = "Format nomor kendaraan tidak valid. Contoh: B 1234 ABC")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jenis kendaraan wajib dipilih")]
        public string VehicleType { get; set; } = string.Empty;

        public string? DriverName { get; set; }

        public string? PhoneNumber { get; set; }
    }
}