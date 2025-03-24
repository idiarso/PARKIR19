using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class ParkingRateConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Jenis Kendaraan")]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif dasar tidak boleh negatif")]
        [Display(Name = "Tarif Dasar")]
        public decimal BaseRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif per jam tidak boleh negatif")]
        [Display(Name = "Tarif per Jam")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif harian tidak boleh negatif")]
        [Display(Name = "Tarif Harian")]
        public decimal DailyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif mingguan tidak boleh negatif")]
        [Display(Name = "Tarif Mingguan")]
        public decimal WeeklyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif bulanan tidak boleh negatif")]
        [Display(Name = "Tarif Bulanan")]
        public decimal MonthlyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif denda tidak boleh negatif")]
        [Display(Name = "Tarif Denda")]
        public decimal PenaltyRate { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Berlaku Sejak")]
        public DateTime EffectiveFrom { get; set; }

        [Display(Name = "Berlaku Sampai")]
        public DateTime? EffectiveTo { get; set; }

        [Required]
        [Display(Name = "Dibuat Oleh")]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Diubah Oleh")]
        public string LastModifiedBy { get; set; } = string.Empty;

        [Display(Name = "Diubah Pada")]
        public DateTime? LastModifiedAt { get; set; }

        // Additional properties referenced in controllers
        [Column(TypeName = "decimal(18,2)")]
        public decimal MotorcycleRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CarRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalHourRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumDailyRate { get; set; }

        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;

        // Custom validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate that daily rate is less than 24 hours of hourly rate
            if (DailyRate >= HourlyRate * 24)
            {
                results.Add(new ValidationResult(
                    "Tarif harian harus lebih murah dari akumulasi tarif per jam selama 24 jam",
                    new[] { nameof(DailyRate) }));
            }

            // Validate that weekly rate is less than 7 days of daily rate
            if (WeeklyRate >= DailyRate * 7)
            {
                results.Add(new ValidationResult(
                    "Tarif mingguan harus lebih murah dari akumulasi tarif harian selama 7 hari",
                    new[] { nameof(WeeklyRate) }));
            }

            // Validate that monthly rate is less than 30 days of daily rate
            if (MonthlyRate >= DailyRate * 30)
            {
                results.Add(new ValidationResult(
                    "Tarif bulanan harus lebih murah dari akumulasi tarif harian selama 30 hari",
                    new[] { nameof(MonthlyRate) }));
            }

            // Validate effective dates
            if (EffectiveTo.HasValue && EffectiveTo.Value <= EffectiveFrom)
            {
                results.Add(new ValidationResult(
                    "Tanggal berlaku akhir harus setelah tanggal berlaku awal",
                    new[] { nameof(EffectiveTo) }));
            }

            return results;
        }
    }
}