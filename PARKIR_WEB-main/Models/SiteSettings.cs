using System;

namespace ParkIRC.Models
{
    public class SiteSettings
    {
        public string Id { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
        public string FaviconPath { get; set; } = string.Empty;
        public string FooterText { get; set; } = string.Empty;
        public string ThemeColor { get; set; } = "#007bff";
        public bool ShowLogo { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
    }
} 