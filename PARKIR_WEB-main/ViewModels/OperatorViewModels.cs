using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParkIRC.Models;

namespace ParkIRC.ViewModels
{
    public class OperatorViewModel
    {
        public Operator Operator { get; set; }
        public string Role { get; set; }
    }
    
    public class CreateOperatorViewModel
    {
        public Operator Operator { get; set; }
        public List<SelectListItem> AvailableRoles { get; set; }
        public string SelectedRole { get; set; }
    }
    
    public class EditOperatorViewModel
    {
        public Operator Operator { get; set; }
        public List<SelectListItem> AvailableRoles { get; set; }
        public string SelectedRole { get; set; }
    }
} 