using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text;
using ParkIRC.ViewModels;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ParkIRC.Services;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class ManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagementController> _logger;
        private readonly UserManager<Operator> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PrintService _printService;
        private readonly string _backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");

        public ManagementController(
            ApplicationDbContext context, 
            ILogger<ManagementController> logger, 
            UserManager<Operator> userManager,
            RoleManager<IdentityRole> roleManager,
            PrintService printService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _printService = printService;
            
            // Pastikan direktori backup ada
            if (!Directory.Exists(_backupDir))
            {
                Directory.CreateDirectory(_backupDir);
            }
        }

        // Parking Slots Management
        public async Task<IActionResult> ParkingSlots()
        {
            var parkingSpaces = await _context.ParkingSpaces.ToListAsync();
            return View(parkingSpaces);
        }

        public IActionResult CreateParkingSlot()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParkingSlot(ParkingSpace parkingSpace)
        {
            if (ModelState.IsValid)
            {
                _context.Add(parkingSpace);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ParkingSlots));
            }
            return View(parkingSpace);
        }

        public async Task<IActionResult> EditParkingSlot(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parkingSpace = await _context.ParkingSpaces.FindAsync(id);
            if (parkingSpace == null)
            {
                return NotFound();
            }
            return View(parkingSpace);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParkingSlot(int id, ParkingSpace parkingSpace)
        {
            if (id != parkingSpace.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(parkingSpace);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParkingSpaceExists(parkingSpace.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ParkingSlots));
            }
            return View(parkingSpace);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParkingSlot(int id)
        {
            var parkingSpace = await _context.ParkingSpaces.FindAsync(id);
            if (parkingSpace != null)
            {
                _context.ParkingSpaces.Remove(parkingSpace);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ParkingSlots));
        }

        private bool ParkingSpaceExists(int id)
        {
            return _context.ParkingSpaces.Any(e => e.Id == id);
        }

        // Operators Management
        public async Task<IActionResult> Operators()
        {
            var operators = await _context.Operators.ToListAsync();
            
            // Add role information
            var operatorsWithRoles = new List<OperatorViewModel>();
            
            foreach (var op in operators)
            {
                var roles = await _userManager.GetRolesAsync(op);
                operatorsWithRoles.Add(new OperatorViewModel 
                {
                    Operator = op,
                    Role = roles.FirstOrDefault() ?? "Staff"
                });
            }
            
            return View(operatorsWithRoles);
        }

        public async Task<IActionResult> CreateOperator()
        {
            // Get available roles
            var roles = await _roleManager.Roles.ToListAsync();
            
            var viewModel = new CreateOperatorViewModel
            {
                Operator = new Operator
                {
                    IsActive = true,
                    JoinDate = DateTime.Today,
                    CreatedAt = DateTime.UtcNow
                },
                AvailableRoles = roles.Select(r => new SelectListItem 
                { 
                    Text = r.Name, 
                    Value = r.Name 
                }).ToList(),
                SelectedRole = "Staff" // Default role
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOperator(CreateOperatorViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Generate a random password for initial setup
                var password = GenerateRandomPassword();
                model.Operator.UserName = model.Operator.Email;
                model.Operator.CreatedAt = DateTime.UtcNow;
                
                var result = await _userManager.CreateAsync(model.Operator, password);
                
                if (result.Succeeded)
                {
                    // Add user to the selected role
                    await _userManager.AddToRoleAsync(model.Operator, model.SelectedRole);
                    
                    // Create a log entry
                    var journal = new Journal
                    {
                        Action = "CREATE_OPERATOR",
                        Description = $"Operator {model.Operator.FullName} dibuat dengan role {model.SelectedRole}",
                        OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system",
                        Timestamp = DateTime.UtcNow
                    };
                    _context.Journals.Add(journal);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Operator berhasil dibuat. Password sementara: {password}";
                    return RedirectToAction(nameof(Operators));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            // If there's an error, repopulate the roles
            var roles = await _roleManager.Roles.ToListAsync();
            model.AvailableRoles = roles.Select(r => new SelectListItem 
            { 
                Text = r.Name, 
                Value = r.Name 
            }).ToList();
            
            return View(model);
        }

        public async Task<IActionResult> EditOperator(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @operator = await _context.Operators.FindAsync(id);
            if (@operator == null)
            {
                return NotFound();
            }
            
            // Get current role
            var roles = await _userManager.GetRolesAsync(@operator);
            var currentRole = roles.FirstOrDefault() ?? "Staff";
            
            // Get all available roles
            var allRoles = await _roleManager.Roles.ToListAsync();
            
            var viewModel = new EditOperatorViewModel
            {
                Operator = @operator,
                AvailableRoles = allRoles.Select(r => new SelectListItem 
                { 
                    Text = r.Name, 
                    Value = r.Name,
                    Selected = r.Name == currentRole
                }).ToList(),
                SelectedRole = currentRole
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOperator(string id, EditOperatorViewModel model)
        {
            if (id != model.Operator.Id.ToString())
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOperator = await _userManager.FindByIdAsync(id);
                    if (existingOperator == null)
                    {
                        return NotFound();
                    }

                    // Debug valores recibidos
                    _logger.LogInformation($"Editando operador: ID={id}, BadgeNumber={model.Operator.BadgeNumber}, PhoneNumber={model.Operator.PhoneNumber}");

                    // Update basic properties
                    existingOperator.FullName = model.Operator.FullName;
                    existingOperator.BadgeNumber = model.Operator.BadgeNumber;
                    existingOperator.PhoneNumber = model.Operator.PhoneNumber;
                    existingOperator.IsActive = model.Operator.IsActive;
                    existingOperator.LastModifiedAt = DateTime.UtcNow;
                    existingOperator.LastModifiedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    
                    // Debug valores después de asignar
                    _logger.LogInformation($"Valores asignados: BadgeNumber={existingOperator.BadgeNumber}, PhoneNumber={existingOperator.PhoneNumber}");
                    
                    var result = await _userManager.UpdateAsync(existingOperator);
                    if (!result.Succeeded)
                    {
                        _logger.LogError($"Error al actualizar operador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        ModelState.AddModelError("", "Error al actualizar el operador");
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                    
                    // Debug después de actualizar
                    _logger.LogInformation("Operador actualizado correctamente");
                    
                    // Update role if changed
                    var currentRoles = await _userManager.GetRolesAsync(existingOperator);
                    if (!currentRoles.Contains(model.SelectedRole))
                    {
                        if (currentRoles.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(existingOperator, currentRoles);
                        }
                        await _userManager.AddToRoleAsync(existingOperator, model.SelectedRole);
                        
                        // Log role change
                        var journal = new Journal
                        {
                            Action = "CHANGE_ROLE",
                            Description = $"Role operator {existingOperator.FullName} diubah menjadi {model.SelectedRole}",
                            OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system",
                            Timestamp = DateTime.UtcNow
                        };
                        _context.Journals.Add(journal);
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["SuccessMessage"] = "Data operator berhasil diupdate";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OperatorExists(model.Operator.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Operators));
            }
            
            // Repopulate roles on error
            var allRoles = await _roleManager.Roles.ToListAsync();
            model.AvailableRoles = allRoles.Select(r => new SelectListItem 
            { 
                Text = r.Name, 
                Value = r.Name,
                Selected = r.Name == model.SelectedRole
            }).ToList();
            
            return View(model);
        }
        
        // Helper method to generate a random password that meets requirements
        private string GenerateRandomPassword()
        {
            const string upperChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijkmnopqrstuvwxyz";
            const string numericChars = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";
            
            var random = new Random();
            var password = new StringBuilder();
            
            // Ensure we have at least one of each type
            password.Append(upperChars[random.Next(upperChars.Length)]);
            password.Append(lowerChars[random.Next(lowerChars.Length)]);
            password.Append(numericChars[random.Next(numericChars.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);
            
            // Add additional characters to get to desired length (minimum 8)
            var allChars = upperChars + lowerChars + numericChars + specialChars;
            for (int i = 0; i < 4; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the characters
            char[] array = password.ToString().ToCharArray();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            
            return new string(array);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOperator(string id)
        {
            var @operator = await _context.Operators.FindAsync(id);
            if (@operator == null)
            {
                return Json(new { success = false, message = "Operator tidak ditemukan" });
            }

            try
            {
                _context.Operators.Remove(@operator);
                await _context.SaveChangesAsync();

                // Log the action
                var journal = new Journal
                {
                    Action = "DELETE_OPERATOR",
                    Description = $"Operator dihapus: {@operator.FullName}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menghapus operator: " + ex.Message });
            }
        }

        private bool OperatorExists(string id)
        {
            return _context.Operators.Any(e => e.Id == id);
        }

        // Shifts Management
        public async Task<IActionResult> Shifts()
        {
            // First get the data without ordering by TimeSpan
            var shifts = await _context.Shifts
                .Include(s => s.Operators)
                .Include(s => s.Vehicles)
                .OrderBy(s => s.Date)
                .ToListAsync();
                
            // Then order by StartTime on the client side
            shifts = shifts
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime.TimeOfDay.TotalMinutes)
                .ToList();
                
            return View(shifts);
        }

        public IActionResult CreateShift()
        {
            var shift = new Shift
            {
                Date = DateTime.Today,
                IsActive = true,
                StartTime = DateTime.Today, // Set default time
                EndTime = DateTime.Today,   // Set default time
                Name = "",                  // Initialize with empty string
                ShiftName = "",             // Initialize with empty string
                Description = "",           // Initialize with empty string
                MaxOperators = 1            // Set default value
            };
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShift(Shift shift, string startTime, string endTime, string[] WorkDays)
        {
            // Log incoming data for debugging
            _logger.LogInformation($"CreateShift POST method called");
            _logger.LogInformation($"Received shift data - Name: '{shift.Name}', IsActive: {shift.IsActive}");
            _logger.LogInformation($"WorkDays count: {(WorkDays?.Length ?? 0)}");
            _logger.LogInformation($"StartTime: '{startTime}', EndTime: '{endTime}'");
            
            // Remove ShiftName validation error if Name is valid
            if (!string.IsNullOrEmpty(shift.Name))
            {
                _logger.LogInformation($"Name is valid: '{shift.Name}'");
                // Remove ShiftName validation error if it exists
                ModelState.Remove("ShiftName");
            }
            else
            {
                ModelState.AddModelError("Name", "Nama shift wajib diisi");
                _logger.LogWarning("Name is null or empty");
            }

            if (WorkDays == null || WorkDays.Length == 0)
            {
                ModelState.AddModelError("WorkDays", "Pilih minimal satu hari kerja");
                _logger.LogWarning("No workdays selected");
            }
            else
            {
                _logger.LogInformation($"Selected workdays: {string.Join(", ", WorkDays)}");
            }

            if (string.IsNullOrEmpty(startTime))
            {
                ModelState.AddModelError("StartTime", "Waktu mulai wajib diisi");
                _logger.LogWarning("StartTime is null or empty");
            }

            if (string.IsNullOrEmpty(endTime))
            {
                ModelState.AddModelError("EndTime", "Waktu selesai wajib diisi");
                _logger.LogWarning("EndTime is null or empty");
            }

            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    // Parse time strings to TimeSpan
                    if (TimeSpan.TryParse(startTime, out TimeSpan parsedStartTime) && 
                        TimeSpan.TryParse(endTime, out TimeSpan parsedEndTime))
                    {
                        var baseDate = DateTime.Today;
                        shift.StartTime = baseDate.Add(parsedStartTime);
                        shift.EndTime = baseDate.Add(parsedEndTime);
                        shift.Date = baseDate;
                        // Set ShiftName from Name
                        shift.ShiftName = shift.Name;
                        _logger.LogInformation($"ShiftName set to: '{shift.ShiftName}'");
                        shift.WorkDaysString = string.Join(",", WorkDays ?? Array.Empty<string>());
                        shift.CreatedAt = DateTime.Now;
                        
                        // Explicitly set IsActive to true for new shifts
                        shift.IsActive = true;

                        _logger.LogInformation($"Adding shift to database: Name='{shift.Name}', ShiftName='{shift.ShiftName}', WorkDaysString='{shift.WorkDaysString}'");
                        
                        _context.Add(shift);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Shift added successfully with ID: {shift.Id}");

                        // Log the action
                        var journal = new Journal
                        {
                            Action = "CREATE_SHIFT",
                            Description = $"Shift baru dibuat: {shift.Name}",
                            OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                            Timestamp = DateTime.UtcNow
                        };
                        _context.Journals.Add(journal);
                        await _context.SaveChangesAsync();

                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            _logger.LogInformation("Returning JSON success response");
                            return Json(new { success = true, message = "Shift berhasil dibuat!" });
                        }

                        _logger.LogInformation("Redirecting to Shifts page");
                        TempData["SuccessMessage"] = "Shift berhasil dibuat!";
                        return RedirectToAction(nameof(Shifts));
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid time format: StartTime='{startTime}', EndTime='{endTime}'");
                        ModelState.AddModelError("", "Format waktu tidak valid");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating shift");
                    ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan shift. Silakan coba lagi.");
                }
            }
            else
            {
                // Log all model state errors for debugging
                _logger.LogWarning("Model validation failed. Errors:");
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogWarning($"Field: {modelState.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning($"Returning JSON error response: {string.Join(", ", errors)}");
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            _logger.LogInformation("Returning to CreateShift view with model");
            return View(shift);
        }

        public async Task<IActionResult> EditShift(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .Include(s => s.Operators)
                .Include(s => s.Vehicles)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (shift == null)
            {
                return NotFound();
            }
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShift(int id, Shift shift, List<string> WorkDays)
        {
            if (id != shift.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingShift = await _context.Shifts
                        .Include(s => s.Operators)
                        .Include(s => s.Vehicles)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (existingShift == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingShift.Name = shift.Name;
                    existingShift.ShiftName = shift.Name;
                    existingShift.StartTime = shift.StartTime;
                    existingShift.EndTime = shift.EndTime;
                    existingShift.Description = shift.Description;
                    existingShift.MaxOperators = shift.MaxOperators;
                    existingShift.IsActive = shift.IsActive;
                    existingShift.WorkDaysString = string.Join(",", WorkDays ?? new List<string>());

                    await _context.SaveChangesAsync();

                    // Log the action
                    var journal = new Journal
                    {
                        Action = "EDIT_SHIFT",
                        Description = $"Shift diperbarui: {shift.Name}",
                        OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                        Timestamp = DateTime.UtcNow
                    };
                    _context.Journals.Add(journal);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Shift berhasil diperbarui!";
                    return RedirectToAction(nameof(Shifts));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                {
                    return Json(new { success = false, message = "Shift tidak ditemukan" });
                }

                _context.Shifts.Remove(shift);

                // Log the action
                var journal = new Journal
                {
                    Action = "DELETE_SHIFT",
                    Description = $"Shift dihapus: {shift.Name}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Shift berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menghapus shift: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleShiftStatus(int id, bool isActive)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                {
                    return Json(new { success = false, message = "Shift tidak ditemukan" });
                }

                shift.IsActive = isActive;
                await _context.SaveChangesAsync();

                // Log the action
                var journal = new Journal
                {
                    Action = isActive ? "ACTIVATE_SHIFT" : "DEACTIVATE_SHIFT",
                    Description = $"Shift {shift.Name} {(isActive ? "diaktifkan" : "dinonaktifkan")}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal memperbarui status shift: " + ex.Message });
            }
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.Id == id);
        }
        
        // Camera Settings Management
        public async Task<IActionResult> CameraSettings()
        {
            try
            {
                _logger.LogInformation("Accessing CameraSettings page");
                
                // Check if table exists
                bool tableExists = await _context.Database.ExecuteSqlRawAsync(
                    "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CameraSettings';") > 0;
                
                if (!tableExists)
                {
                    _logger.LogError("CameraSettings table does not exist in the database");
                    return Content("Database error: CameraSettings table does not exist. Please contact administrator.");
                }
                
                var cameraSettings = await _context.CameraSettings.ToListAsync();
                _logger.LogInformation($"Retrieved {cameraSettings.Count} camera settings");
                return View(cameraSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving camera settings");
                return Content($"An error occurred: {ex.Message}\n\nStack trace: {ex.StackTrace}");
            }
        }
        
        public IActionResult CreateCameraSetting()
        {
            return View(new CameraSettings
            {
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCameraSetting(CameraSettings cameraSettings)
        {
            if (ModelState.IsValid)
            {
                cameraSettings.CreatedAt = DateTime.UtcNow;
                _context.Add(cameraSettings);
                await _context.SaveChangesAsync();
                
                // Log the action
                var journal = new Journal
                {
                    Action = "CREATE_CAMERA_SETTING",
                    Description = $"Pengaturan kamera dibuat: {cameraSettings.ProfileName}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(CameraSettings));
            }
            return View(cameraSettings);
        }
        
        public async Task<IActionResult> EditCameraSetting(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cameraSetting = await _context.CameraSettings.FindAsync(id);
            if (cameraSetting == null)
            {
                return NotFound();
            }
            return View(cameraSetting);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCameraSetting(int id, CameraSettings cameraSettings)
        {
            if (id != cameraSettings.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    cameraSettings.ModifiedAt = DateTime.UtcNow;
                    _context.Update(cameraSettings);
                    await _context.SaveChangesAsync();
                    
                    // Log the action
                    var journal = new Journal
                    {
                        Action = "UPDATE_CAMERA_SETTING",
                        Description = $"Pengaturan kamera diperbarui: {cameraSettings.ProfileName}",
                        OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                        Timestamp = DateTime.UtcNow
                    };
                    _context.Journals.Add(journal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CameraSettingExists(cameraSettings.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(CameraSettings));
            }
            return View(cameraSettings);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCameraSetting(int id)
        {
            var cameraSetting = await _context.CameraSettings.FindAsync(id);
            if (cameraSetting == null)
            {
                return Json(new { success = false, message = "Pengaturan kamera tidak ditemukan" });
            }

            try
            {
                _context.CameraSettings.Remove(cameraSetting);
                await _context.SaveChangesAsync();

                // Log the action
                var journal = new Journal
                {
                    Action = "DELETE_CAMERA_SETTING",
                    Description = $"Pengaturan kamera dihapus: {cameraSetting.ProfileName}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menghapus pengaturan kamera: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleCameraSettingStatus(int id, bool isActive)
        {
            var cameraSetting = await _context.CameraSettings.FindAsync(id);
            if (cameraSetting == null)
            {
                return Json(new { success = false, message = "Pengaturan kamera tidak ditemukan" });
            }

            try
            {
                cameraSetting.IsActive = isActive;
                cameraSetting.ModifiedAt = DateTime.UtcNow;
                
                _context.Update(cameraSetting);
                await _context.SaveChangesAsync();

                // Log the action
                var status = isActive ? "diaktifkan" : "dinonaktifkan";
                var journal = new Journal
                {
                    Action = "TOGGLE_CAMERA_SETTING",
                    Description = $"Pengaturan kamera {cameraSetting.ProfileName} {status}",
                    OperatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    Timestamp = DateTime.UtcNow
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gagal {(isActive ? "mengaktifkan" : "menonaktifkan")} pengaturan kamera: {ex.Message}" });
            }
        }
        
        private bool CameraSettingExists(int id)
        {
            return _context.CameraSettings.Any(e => e.Id == id);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet("Backup")]
        public IActionResult Backup()
        {
            var model = new BackupViewModel
            {
                BackupOptions = new List<string> { "Database", "Uploads", "Configuration", "Complete" },
                AvailableBackups = GetAvailableBackups()
            };
            
            return View(model);
        }
        
        [HttpPost("Backup")]
        public async Task<IActionResult> CreateBackup(BackupViewModel model)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"backup_{timestamp}.zip";
                string backupPath = Path.Combine(_backupDir, backupFileName);
                
                // Simpan file log
                string logPath = Path.Combine(_backupDir, $"backup_log_{timestamp}.txt");
                using (StreamWriter writer = new StreamWriter(logPath))
                {
                    writer.WriteLine($"Backup started at {DateTime.Now}");
                    
                    // Jika "Complete" atau "Database" dipilih
                    if (model.SelectedOptions.Contains("Complete") || model.SelectedOptions.Contains("Database"))
                    {
                        writer.WriteLine("Backing up database...");
                        
                        string tempDbPath = Path.Combine(Path.GetTempPath(), $"parkir_db_{timestamp}.dump");
                        
                        // Backup database menggunakan pg_dump
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "pg_dump",
                            Arguments = $"-Fc parkir_db > {tempDbPath}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using (var process = Process.Start(processInfo))
                        {
                            process.WaitForExit();
                            string output = await process.StandardOutput.ReadToEndAsync();
                            string error = await process.StandardError.ReadToEndAsync();
                            
                            if (!string.IsNullOrEmpty(error))
                            {
                                writer.WriteLine($"Database backup error: {error}");
                                _logger.LogError($"Database backup error: {error}");
                                TempData["Error"] = "Error backing up database";
                                return RedirectToAction(nameof(Backup));
                            }
                            
                            writer.WriteLine("Database backup completed");
                        }
                    }
                    
                    // Membuat ZIP file untuk backup
                    using (var zipFile = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                    {
                        if (model.SelectedOptions.Contains("Complete") || model.SelectedOptions.Contains("Database"))
                        {
                            string tempDbPath = Path.Combine(Path.GetTempPath(), $"parkir_db_{timestamp}.dump");
                            if (System.IO.File.Exists(tempDbPath))
                            {
                                zipFile.CreateEntryFromFile(tempDbPath, "database.dump");
                                writer.WriteLine($"Added database dump to backup");
                                System.IO.File.Delete(tempDbPath); // Hapus file temporary
                            }
                        }
                        
                        if (model.SelectedOptions.Contains("Complete") || model.SelectedOptions.Contains("Uploads"))
                        {
                            string uploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "uploads");
                            if (Directory.Exists(uploadsDir))
                            {
                                foreach (var file in Directory.GetFiles(uploadsDir, "*", SearchOption.AllDirectories))
                                {
                                    string relativePath = file.Substring(uploadsDir.Length + 1);
                                    zipFile.CreateEntryFromFile(file, Path.Combine("uploads", relativePath));
                                }
                                writer.WriteLine($"Added uploads to backup");
                            }
                        }
                        
                        if (model.SelectedOptions.Contains("Complete") || model.SelectedOptions.Contains("Configuration"))
                        {
                            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                            if (System.IO.File.Exists(configFile))
                            {
                                zipFile.CreateEntryFromFile(configFile, "appsettings.json");
                                writer.WriteLine($"Added configuration to backup");
                            }
                        }
                    }
                    
                    writer.WriteLine($"Backup completed at {DateTime.Now}");
                }
                
                TempData["Success"] = $"Backup created successfully: {backupFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                TempData["Error"] = $"Error creating backup: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Backup));
        }
        
        [HttpGet("Restore")]
        public IActionResult Restore()
        {
            var model = new RestoreViewModel
            {
                AvailableBackups = GetAvailableBackups()
            };
            
            return View(model);
        }
        
        [HttpPost("Restore")]
        public async Task<IActionResult> PerformRestore(RestoreViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.SelectedBackup))
                {
                    TempData["Error"] = "No backup selected";
                    return RedirectToAction(nameof(Restore));
                }
                
                string backupPath = Path.Combine(_backupDir, model.SelectedBackup);
                if (!System.IO.File.Exists(backupPath))
                {
                    TempData["Error"] = "Selected backup file does not exist";
                    return RedirectToAction(nameof(Restore));
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string tempDir = Path.Combine(Path.GetTempPath(), $"parkirc_restore_{timestamp}");
                Directory.CreateDirectory(tempDir);
                
                // Ekstrak backup
                ZipFile.ExtractToDirectory(backupPath, tempDir);
                
                // Restore database jika ada
                string dbDumpPath = Path.Combine(tempDir, "database.dump");
                if (System.IO.File.Exists(dbDumpPath) && model.RestoreDatabase)
                {
                    // Restore database menggunakan pg_restore
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "pg_restore",
                        Arguments = $"-d parkir_db {dbDumpPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using (var process = Process.Start(processInfo))
                    {
                        process.WaitForExit();
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        
                        if (!string.IsNullOrEmpty(error) && !error.Contains("already exists"))
                        {
                            _logger.LogError($"Database restore error: {error}");
                            TempData["Error"] = "Error restoring database";
                            return RedirectToAction(nameof(Restore));
                        }
                    }
                }
                
                // Restore uploads jika ada
                string uploadsBackupDir = Path.Combine(tempDir, "uploads");
                if (Directory.Exists(uploadsBackupDir) && model.RestoreUploads)
                {
                    string targetUploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "uploads");
                    if (!Directory.Exists(targetUploadsDir))
                    {
                        Directory.CreateDirectory(targetUploadsDir);
                    }
                    
                    foreach (var file in Directory.GetFiles(uploadsBackupDir, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = file.Substring(uploadsBackupDir.Length + 1);
                        string targetPath = Path.Combine(targetUploadsDir, relativePath);
                        
                        // Pastikan direktori target ada
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        
                        System.IO.File.Copy(file, targetPath, true);
                    }
                }
                
                // Restore configuration jika ada
                string configBackupPath = Path.Combine(tempDir, "appsettings.json");
                if (System.IO.File.Exists(configBackupPath) && model.RestoreConfig)
                {
                    string targetConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    System.IO.File.Copy(configBackupPath, targetConfigPath, true);
                }
                
                // Bersihkan temporary files
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                TempData["Success"] = "Restore completed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup");
                TempData["Error"] = $"Error restoring backup: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Restore));
        }
        
        [HttpGet("Printer")]
        public IActionResult Printer()
        {
            var printers = GetAvailablePrinters();
            var model = new PrinterManagementViewModel
            {
                AvailablePrinters = printers,
                CurrentPrinter = _printService.GetCurrentPrinter()
            };
            
            return View(model);
        }
        
        [HttpPost("SetPrinter")]
        public IActionResult SetPrinter(string printerName)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "lpoptions",
                    Arguments = $"-d {printerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string error = process.StandardError.ReadToEnd();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogError($"Error setting printer: {error}");
                        TempData["Error"] = $"Error setting printer: {error}";
                        return RedirectToAction(nameof(Printer));
                    }
                }
                
                TempData["Success"] = $"Default printer set to {printerName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting printer");
                TempData["Error"] = $"Error setting printer: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Printer));
        }
        
        [HttpPost("TestPrint")]
        public IActionResult TestPrint(string printerName)
        {
            try
            {
                var testContent = "========== TEST PRINT ==========\n" +
                                  "ParkIRC System\n" +
                                  "Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                                  "Printer: " + printerName + "\n" +
                                  "================================\n\n\n\n\n";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"echo '{testContent}' | lp -d {printerName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogError($"Error test printing: {error}");
                        TempData["Error"] = $"Error test printing: {error}";
                        return RedirectToAction(nameof(Printer));
                    }
                }
                
                TempData["Success"] = "Test print sent successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test printing");
                TempData["Error"] = $"Error test printing: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Printer));
        }
        
        [HttpGet("System")]
        public IActionResult SystemStatus()
        {
            var model = GetSystemStatus();
            return View(model);
        }
        
        [HttpGet("DownloadBackup/{filename}")]
        public IActionResult DownloadBackup(string filename)
        {
            try
            {
                var backupPath = Path.Combine(_backupDir, filename);
                if (!System.IO.File.Exists(backupPath))
                {
                    TempData["Error"] = "Backup file not found";
                    return RedirectToAction(nameof(Backup));
                }
                
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(backupPath, FileMode.Open))
                {
                    fileStream.CopyTo(memoryStream);
                }
                memoryStream.Position = 0;
                
                return File(memoryStream, "application/zip", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup");
                TempData["Error"] = $"Error downloading backup: {ex.Message}";
                return RedirectToAction(nameof(Backup));
            }
        }
        
        [HttpPost("DeleteBackup/{filename}")]
        public IActionResult DeleteBackup(string filename)
        {
            try
            {
                var backupPath = Path.Combine(_backupDir, filename);
                if (!System.IO.File.Exists(backupPath))
                {
                    TempData["Error"] = "Backup file not found";
                    return RedirectToAction(nameof(Backup));
                }
                
                System.IO.File.Delete(backupPath);
                TempData["Success"] = $"Backup file {filename} deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup");
                TempData["Error"] = $"Error deleting backup: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Backup));
        }
        
        private List<string> GetAvailableBackups()
        {
            return Directory.GetFiles(_backupDir, "backup_*.zip")
                .Select(Path.GetFileName)
                .OrderByDescending(x => x)
                .ToList();
        }
        
        private List<string> GetAvailablePrinters()
        {
            try
            {
                var printers = new List<string>();
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("printer "))
                        {
                            var parts = line.Split(' ');
                            if (parts.Length > 1)
                            {
                                printers.Add(parts[1]);
                            }
                        }
                    }
                }
                
                return printers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available printers");
                return new List<string>();
            }
        }
        
        private SystemStatusViewModel GetSystemStatus()
        {
            var model = new SystemStatusViewModel();
            
            try
            {
                // Cek database
                model.DatabaseStatus = _context.Database.CanConnect();
                
                // Cek PostgreSQL service
                var pgProcess = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "is-active postgresql",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(pgProcess))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    model.PostgresServiceStatus = output == "active";
                }
                
                // Cek CUPS service
                var cupsProcess = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "is-active cups",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(cupsProcess))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    model.CupsServiceStatus = output == "active";
                }
                
                // Dapatkan disk usage
                var diskProcess = new ProcessStartInfo
                {
                    FileName = "df",
                    Arguments = "-h .",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(diskProcess))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    if (lines.Length > 1)
                    {
                        var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 4)
                        {
                            model.DiskTotal = parts[1];
                            model.DiskUsed = parts[2];
                            model.DiskFree = parts[3];
                            model.DiskUsagePercent = parts[4];
                        }
                    }
                }
                
                // Dapatkan memory usage
                var memProcess = new ProcessStartInfo
                {
                    FileName = "free",
                    Arguments = "-h",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(memProcess))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n');
                    if (lines.Length > 1)
                    {
                        var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 6)
                        {
                            model.MemoryTotal = parts[1];
                            model.MemoryUsed = parts[2];
                            model.MemoryFree = parts[3];
                        }
                    }
                }
                
                // Dapatkan uptime
                var uptimeProcess = new ProcessStartInfo
                {
                    FileName = "uptime",
                    Arguments = "-p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(uptimeProcess))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    model.SystemUptime = output;
                }
                
                // Dapatkan load average
                var loadavgProcess = new ProcessStartInfo
                {
                    FileName = "cat",
                    Arguments = "/proc/loadavg",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(loadavgProcess))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    var parts = output.Split(' ');
                    if (parts.Length > 2)
                    {
                        model.LoadAverage1 = parts[0];
                        model.LoadAverage5 = parts[1];
                        model.LoadAverage15 = parts[2];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                model.ErrorMessage = ex.Message;
            }
            
            return model;
        }

        [HttpGet]
        public async Task<IActionResult> VehicleHistory(
            string status = null,
            DateTime? startDate = null, 
            DateTime? endDate = null,
            string vehicleType = null,
            string plateNumber = null,
            int page = 1)
        {
            var query = _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .AsQueryable();

            // Filter by status (In/Out)
            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "in")
                {
                    query = query.Where(t => t.ExitTime == null);
                }
                else if (status.ToLower() == "out")
                {
                    query = query.Where(t => t.ExitTime != null);
                }
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(t => t.EntryTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.EntryTime <= endDate.Value);
            }

            // Filter by vehicle type
            if (!string.IsNullOrEmpty(vehicleType))
            {
                query = query.Where(t => t.Vehicle.VehicleType == vehicleType);
            }

            // Filter by plate number
            if (!string.IsNullOrEmpty(plateNumber))
            {
                query = query.Where(t => t.Vehicle.VehicleNumber.Contains(plateNumber));
            }

            // Order by latest first
            query = query.OrderByDescending(t => t.EntryTime);

            // Pagination
            int pageSize = 20;
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new VehicleHistoryViewModel
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    VehicleNumber = t.Vehicle.VehicleNumber,
                    VehicleType = t.Vehicle.VehicleType,
                    EntryTime = t.EntryTime,
                    ExitTime = t.ExitTime,
                    Duration = t.ExitTime.HasValue ? 
                        t.ExitTime.Value - t.EntryTime : 
                        TimeSpan.Zero,
                    Status = t.ExitTime.HasValue ? "Keluar" : "Masuk",
                    TotalAmount = t.TotalAmount
                })
                .ToListAsync();

            var model = new VehicleHistoryPageViewModel
            {
                Transactions = transactions,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize),
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                VehicleType = vehicleType,
                PlateNumber = plateNumber
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportVehicleHistory(
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string vehicleType = null,
            string plateNumber = null,
            string format = "excel")
        {
            // Similar query building as above
            var query = _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .AsQueryable();
            
            // Apply filters...

            var data = await query
                .Select(t => new
                {
                    t.TicketNumber,
                    t.Vehicle.VehicleNumber,
                    t.Vehicle.VehicleType,
                    EntryTime = t.EntryTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    ExitTime = t.ExitTime.HasValue ? 
                        t.ExitTime.Value.ToString("dd/MM/yyyy HH:mm:ss") : "-",
                    Duration = t.ExitTime.HasValue ?
                        $"{(t.ExitTime.Value - t.EntryTime).TotalHours:F1} jam" : "-",
                    Status = t.ExitTime.HasValue ? "Keluar" : "Masuk",
                    Amount = t.TotalAmount.ToString("C")
                })
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                // Return Excel file
                return File(GenerateExcel(data), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"vehicle-history-{DateTime.Now:yyyyMMdd}.xlsx");
            }
            else
            {
                // Return PDF file
                return File(GeneratePdf(data),
                    "application/pdf",
                    $"vehicle-history-{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateParkingRates(ParkingRateViewModel model)
        {
            try
            {
                var rates = await _context.ParkingRates.FirstOrDefaultAsync();
                if (rates == null)
                {
                    rates = new ParkingRates();
                    _context.ParkingRates.Add(rates);
                }

                rates.MotorcycleRate = model.MotorcycleRate;
                rates.CarRate = model.CarRate;
                rates.AdditionalHourRate = model.AdditionalHourRate;
                rates.MaximumDailyRate = model.MaximumDailyRate;
                rates.LastUpdated = DateTime.Now;
                rates.UpdatedBy = User.Identity.Name;

                await _context.SaveChangesAsync();
                TempData["Message"] = "Tarif parkir berhasil diupdate";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parking rates");
                return View(model);
            }
        }
    }
} 