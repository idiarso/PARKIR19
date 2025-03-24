using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParkIRC.Models;

namespace ParkIRC.ViewModels
{
    public class OperatorViewModel
    {
        public Operator Operator { get; set; } = new Operator();
        public string Role { get; set; } = string.Empty;
    }
    
    public class CreateOperatorViewModel
    {
        public Operator Operator { get; set; } = new Operator();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public string SelectedRole { get; set; } = string.Empty;
    }
    
    public class EditOperatorViewModel
    {
        public Operator Operator { get; set; } = new Operator();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public string SelectedRole { get; set; } = string.Empty;
    }
} 