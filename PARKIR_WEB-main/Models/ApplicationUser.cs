using Microsoft.AspNetCore.Identity;

namespace ParkIRC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
} 