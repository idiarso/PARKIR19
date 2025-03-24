using System;
using System.Threading.Tasks;
using SimpleParkingAdmin.Models;
using Npgsql;

namespace SimpleParkingAdmin.Hardware
{
    /// <summary>
    /// Example usage of HardwareManager for ParkingIN application
    /// </summary>
    public class ParkingEntryHandler
    {
        private readonly HardwareManager _hardwareManager;
        private readonly string _connectionString;
        private readonly string _operatorName;

        public ParkingEntryHandler(string operatorName)
        {
            _hardwareManager = HardwareManager.Instance;
            _connectionString = "Host=localhost;Port=5432;Database=parkirdb;Username=postgres;Password=root@rsi;";
            _operatorName = operatorName;

            // Subscribe to hardware events
            _hardwareManager.CommandReceived += HardwareManager_CommandReceived;
            _hardwareManager.ImageCaptured += HardwareManager_ImageCaptured;

            // Initialize hardware
            if (!_hardwareManager.Initialize())
            {
                Console.WriteLine("Failed to initialize hardware. Some features may not work.");
            }
        }

        /// <summary>
        /// Handles button press from hardware
        /// </summary>
        private async void HardwareManager_CommandReceived(object sender, CommandReceivedEventArgs e)
        {
            try
            {
                if (e.Command == "IN")
                {
                    Console.WriteLine($"Entry button pressed: {e.Data}");
                    
                    // Generate ticket number based on current timestamp
                    string ticketNumber = $"P{DateTime.Now:yyyyMMddHHmmss}";
                    
                    // Take picture
                    string imagePath = await _hardwareManager.CaptureEntryImageAsync(ticketNumber);
                    
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        // Save entry data to database
                        var entryData = new Vehicle
                        {
                            TicketNumber = ticketNumber,
                            EntryTime = DateTime.Now,
                            EntryImagePath = imagePath,
                            VehicleTypeId = 1, // Default vehicle type, would be set based on actual detection
                            CreatedBy = _operatorName
                        };
                        
                        // Insert into database
                        await SaveEntryDataAsync(entryData);
                        
                        // Print ticket
                        // In a real implementation, this would call a ticket printing service
                        Console.WriteLine($"Printing ticket: {ticketNumber}");
                        
                        // Open gate
                        _hardwareManager.OpenEntryGate();
                        
                        Console.WriteLine("Entry process completed successfully");
                    }
                    else
                    {
                        Console.WriteLine("Failed to capture entry image");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing entry command: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles image capture events
        /// </summary>
        private void HardwareManager_ImageCaptured(object sender, ImageCapturedEventArgs e)
        {
            Console.WriteLine($"Image captured for ticket {e.TicketId}: {e.ImagePath}");
        }

        /// <summary>
        /// Saves entry data to database
        /// </summary>
        private async Task SaveEntryDataAsync(Vehicle vehicle)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        INSERT INTO vehicles 
                        (ticket_number, entry_time, vehicle_type_id, entry_image_path, created_by, created_at, is_active) 
                        VALUES 
                        (@ticketNumber, @entryTime, @vehicleTypeId, @entryImagePath, @createdBy, @createdAt, @isActive)";
                    
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("ticketNumber", vehicle.TicketNumber);
                        command.Parameters.AddWithValue("entryTime", vehicle.EntryTime);
                        command.Parameters.AddWithValue("vehicleTypeId", vehicle.VehicleTypeId);
                        command.Parameters.AddWithValue("entryImagePath", vehicle.EntryImagePath);
                        command.Parameters.AddWithValue("createdBy", vehicle.CreatedBy);
                        command.Parameters.AddWithValue("createdAt", DateTime.Now);
                        command.Parameters.AddWithValue("isActive", true);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                Console.WriteLine($"Entry data saved for ticket: {vehicle.TicketNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving entry data: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Example usage of HardwareManager for ParkingOUT application
    /// </summary>
    public class ParkingExitHandler
    {
        private readonly HardwareManager _hardwareManager;
        private readonly string _connectionString;
        private readonly string _operatorName;

        public ParkingExitHandler(string operatorName)
        {
            _hardwareManager = HardwareManager.Instance;
            _connectionString = "Host=localhost;Port=5432;Database=parkirdb;Username=postgres;Password=root@rsi;";
            _operatorName = operatorName;

            // Subscribe to hardware events
            _hardwareManager.CommandReceived += HardwareManager_CommandReceived;

            // Initialize hardware
            if (!_hardwareManager.Initialize())
            {
                Console.WriteLine("Failed to initialize hardware. Some features may not work.");
            }
        }

        /// <summary>
        /// Handles barcode scan
        /// </summary>
        public async Task ProcessBarcodeAsync(string ticketNumber)
        {
            try
            {
                Console.WriteLine($"Processing barcode: {ticketNumber}");
                
                // Retrieve entry data from database
                var vehicle = await RetrieveVehicleDataAsync(ticketNumber);
                
                if (vehicle != null)
                {
                    // Display vehicle information and entry image for verification
                    Console.WriteLine($"Vehicle found: {vehicle.TicketNumber}");
                    Console.WriteLine($"Entry time: {vehicle.EntryTime}");
                    Console.WriteLine($"Entry image: {vehicle.EntryImagePath}");
                    
                    // In a real implementation, this would display in the UI
                }
                else
                {
                    Console.WriteLine($"No vehicle found with ticket number: {ticketNumber}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing barcode: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens gate after payment verification
        /// </summary>
        public async Task CompleteExitAsync(string ticketNumber, decimal fee, int paymentMethodId)
        {
            try
            {
                // Take exit picture
                string exitImagePath = await _hardwareManager.CaptureExitImageAsync(ticketNumber);
                
                // Update vehicle record
                await UpdateExitDataAsync(ticketNumber, exitImagePath, fee, paymentMethodId);
                
                // Open exit gate
                _hardwareManager.OpenExitGate();
                
                Console.WriteLine($"Exit process completed for ticket: {ticketNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing exit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles button press from hardware
        /// </summary>
        private void HardwareManager_CommandReceived(object sender, CommandReceivedEventArgs e)
        {
            try
            {
                if (e.Command == "OUT")
                {
                    Console.WriteLine($"Exit button pressed: {e.Data}");
                    // In a real implementation, this would trigger barcode scanning or manual ticket entry
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing exit command: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves vehicle data from database
        /// </summary>
        private async Task<Vehicle> RetrieveVehicleDataAsync(string ticketNumber)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT id, ticket_number, entry_time, vehicle_type_id, entry_image_path, created_by 
                        FROM vehicles 
                        WHERE ticket_number = @ticketNumber AND is_active = true AND exit_time IS NULL";
                    
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("ticketNumber", ticketNumber);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Vehicle
                                {
                                    Id = reader.GetInt32(0),
                                    TicketNumber = reader.GetString(1),
                                    EntryTime = reader.GetDateTime(2),
                                    VehicleTypeId = reader.GetInt32(3),
                                    EntryImagePath = reader.GetString(4),
                                    CreatedBy = reader.GetString(5)
                                };
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving vehicle data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates exit data in database
        /// </summary>
        private async Task UpdateExitDataAsync(string ticketNumber, string exitImagePath, decimal fee, int paymentMethodId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        UPDATE vehicles 
                        SET exit_time = @exitTime, 
                            exit_image_path = @exitImagePath, 
                            fee = @fee, 
                            payment_method_id = @paymentMethodId, 
                            updated_by = @updatedBy, 
                            updated_at = @updatedAt 
                        WHERE ticket_number = @ticketNumber AND is_active = true";
                    
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("exitTime", DateTime.Now);
                        command.Parameters.AddWithValue("exitImagePath", exitImagePath);
                        command.Parameters.AddWithValue("fee", fee);
                        command.Parameters.AddWithValue("paymentMethodId", paymentMethodId);
                        command.Parameters.AddWithValue("updatedBy", _operatorName);
                        command.Parameters.AddWithValue("updatedAt", DateTime.Now);
                        command.Parameters.AddWithValue("ticketNumber", ticketNumber);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                Console.WriteLine($"Exit data updated for ticket: {ticketNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating exit data: {ex.Message}");
                throw;
            }
        }
    }
}
