using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.Models
{
    public class CameraSettings
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Nama profil wajib diisi")]
        [Display(Name = "Nama Profil")]
        public string ProfileName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Resolusi width wajib diisi")]
        [Range(320, 3840, ErrorMessage = "Width harus berada antara 320 dan 3840")]
        [Display(Name = "Width")]
        public int ResolutionWidth { get; set; } = 1280;
        
        [Required(ErrorMessage = "Resolusi height wajib diisi")]
        [Range(240, 2160, ErrorMessage = "Height harus berada antara 240 dan 2160")]
        [Display(Name = "Height")]
        public int ResolutionHeight { get; set; } = 720;
        
        [Display(Name = "ROI X")]
        public int ROI_X { get; set; } = 0;
        
        [Display(Name = "ROI Y")]
        public int ROI_Y { get; set; } = 0;
        
        [Display(Name = "ROI Width")]
        public int ROI_Width { get; set; } = 640;
        
        [Display(Name = "ROI Height")]
        public int ROI_Height { get; set; } = 480;
        
        [Range(30, 200, ErrorMessage = "Nilai exposure harus berada antara 30 dan 200")]
        [Display(Name = "Exposure")]
        public int Exposure { get; set; } = 100;
        
        [Range(1.0, 2.5, ErrorMessage = "Nilai gain harus berada antara 1.0 dan 2.5")]
        [Display(Name = "Gain")]
        public double Gain { get; set; } = 1.0;
        
        [Range(35, 65, ErrorMessage = "Nilai brightness harus berada antara 35 dan 65")]
        [Display(Name = "Brightness")]
        public int Brightness { get; set; } = 50;
        
        [Range(40, 60, ErrorMessage = "Nilai contrast harus berada antara 40 dan 60")]
        [Display(Name = "Contrast")]
        public int Contrast { get; set; } = 50;
        
        [Display(Name = "Kondisi Pencahayaan")]
        public string LightingCondition { get; set; } = "Normal"; // Normal, LowLight, BrightLight
        
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Diubah Pada")]
        public DateTime? ModifiedAt { get; set; }
    }
} 