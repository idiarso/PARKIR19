namespace ParkIRC.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
} 