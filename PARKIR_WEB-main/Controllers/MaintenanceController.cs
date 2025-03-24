using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using System;
using System.Threading.Tasks;
using System.Text;

namespace ParkIRC.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(ApplicationDbContext context, ILogger<MaintenanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> ApplyMigrations()
        {
            try
            {
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
                return Content("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying migrations");
                return Content($"Error applying migrations: {ex.Message}");
            }
        }
        
        public async Task<IActionResult> CheckDb()
        {
            try
            {
                bool cameraSettingsTableExists = await _context.Database.ExecuteSqlRawAsync("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CameraSettings';") > 0;
                string message = cameraSettingsTableExists ? 
                    "CameraSettings table exists" : 
                    "CameraSettings table does not exist";
                    
                _logger.LogInformation(message);
                return Content(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database");
                return Content($"Error checking database: {ex.Message}");
            }
        }

        public async Task<IActionResult> CreateCameraSettingsTable()
        {
            try
            {
                // First check if the table already exists
                var tableExists = await _context.Database.ExecuteSqlRawAsync(
                    "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CameraSettings';") > 0;
                
                if (tableExists)
                {
                    _logger.LogInformation("CameraSettings table already exists");
                    return Content("CameraSettings table already exists");
                }
                
                // Create the table using raw SQL
                await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE CameraSettings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProfileName TEXT NOT NULL,
                    ResolutionWidth INTEGER NOT NULL DEFAULT 1280,
                    ResolutionHeight INTEGER NOT NULL DEFAULT 720,
                    ROI_X INTEGER NOT NULL DEFAULT 0,
                    ROI_Y INTEGER NOT NULL DEFAULT 0,
                    ROI_Width INTEGER NOT NULL DEFAULT 640,
                    ROI_Height INTEGER NOT NULL DEFAULT 480,
                    Exposure INTEGER NOT NULL DEFAULT 100,
                    Gain REAL NOT NULL DEFAULT 1.0,
                    Brightness INTEGER NOT NULL DEFAULT 50,
                    Contrast INTEGER NOT NULL DEFAULT 50,
                    LightingCondition TEXT NOT NULL DEFAULT 'Normal',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    ModifiedAt TEXT NULL
                )");
                
                // Update the migrations history table to record this migration
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250308180000_AddCameraSettings', '7.0.0')");
                
                _logger.LogInformation("CameraSettings table created successfully");
                return Content("CameraSettings table created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CameraSettings table");
                return Content($"Error creating CameraSettings table: {ex.Message}");
            }
        }

        public async Task<IActionResult> DiagnoseDatabase()
        {
            try
            {
                var result = new StringBuilder();
                result.AppendLine("Database Diagnostic Report");
                result.AppendLine("------------------------");
                
                // Check if database is connected
                bool canConnect = _context.Database.CanConnect();
                result.AppendLine($"Database connection: {(canConnect ? "OK" : "FAILED")}");
                
                if (!canConnect)
                {
                    return Content(result.ToString());
                }
                
                // List all tables in the database
                var tableListQuery = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = tableListQuery;
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        result.AppendLine("\nTables in database:");
                        int tableCount = 0;
                        
                        while (await reader.ReadAsync())
                        {
                            string tableName = reader.GetString(0);
                            result.AppendLine($"- {tableName}");
                            tableCount++;
                            
                            // Specifically check for CameraSettings table
                            if (tableName == "CameraSettings")
                            {
                                result.AppendLine($"  Found CameraSettings table!");
                            }
                        }
                        
                        result.AppendLine($"\nTotal tables: {tableCount}");
                    }
                }
                
                // Check if the CameraSettings table is in __EFMigrationsHistory
                bool migrationApplied = false;
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20250308180000_AddCameraSettings';";
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    var count = await command.ExecuteScalarAsync();
                    migrationApplied = (long)count > 0;
                }
                
                result.AppendLine($"\nCameraSettings migration in __EFMigrationsHistory: {(migrationApplied ? "Yes" : "No")}");
                
                return Content(result.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing database");
                return Content($"Error diagnosing database: {ex.Message}");
            }
        }

        public async Task<IActionResult> FixCameraSettings()
        {
            try
            {
                var result = new StringBuilder();
                result.AppendLine("Attempting to fix CameraSettings table");
                result.AppendLine("-----------------------------------");
                
                // 1. Check if migration record exists but table doesn't
                bool migrationApplied = false;
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20250308180000_AddCameraSettings';";
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    var count = await command.ExecuteScalarAsync();
                    migrationApplied = (long)count > 0;
                }
                
                // 2. Check if table exists
                bool tableExists = false;
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CameraSettings';";
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    var count = await command.ExecuteScalarAsync();
                    tableExists = (long)count > 0;
                }
                
                result.AppendLine($"Migration record exists: {migrationApplied}");
                result.AppendLine($"Table exists: {tableExists}");
                
                // 3. Fix based on findings
                if (migrationApplied && !tableExists)
                {
                    // Migration record exists but table doesn't - create table
                    result.AppendLine("\nCreating missing CameraSettings table...");
                    
                    await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE CameraSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProfileName TEXT NOT NULL,
                        ResolutionWidth INTEGER NOT NULL DEFAULT 1280,
                        ResolutionHeight INTEGER NOT NULL DEFAULT 720,
                        ROI_X INTEGER NOT NULL DEFAULT 0,
                        ROI_Y INTEGER NOT NULL DEFAULT 0,
                        ROI_Width INTEGER NOT NULL DEFAULT 640,
                        ROI_Height INTEGER NOT NULL DEFAULT 480,
                        Exposure INTEGER NOT NULL DEFAULT 100,
                        Gain REAL NOT NULL DEFAULT 1.0,
                        Brightness INTEGER NOT NULL DEFAULT 50,
                        Contrast INTEGER NOT NULL DEFAULT 50,
                        LightingCondition TEXT NOT NULL DEFAULT 'Normal',
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        ModifiedAt TEXT NULL
                    )");
                    
                    result.AppendLine("Table created successfully");
                }
                else if (!migrationApplied && tableExists)
                {
                    // Table exists but migration record doesn't - add migration record
                    result.AppendLine("\nAdding missing migration record...");
                    
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250308180000_AddCameraSettings', '7.0.0')");
                    
                    result.AppendLine("Migration record added successfully");
                }
                else if (!migrationApplied && !tableExists)
                {
                    // Neither exists - create both
                    result.AppendLine("\nCreating both table and migration record...");
                    
                    await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE CameraSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProfileName TEXT NOT NULL,
                        ResolutionWidth INTEGER NOT NULL DEFAULT 1280,
                        ResolutionHeight INTEGER NOT NULL DEFAULT 720,
                        ROI_X INTEGER NOT NULL DEFAULT 0,
                        ROI_Y INTEGER NOT NULL DEFAULT 0,
                        ROI_Width INTEGER NOT NULL DEFAULT 640,
                        ROI_Height INTEGER NOT NULL DEFAULT 480,
                        Exposure INTEGER NOT NULL DEFAULT 100,
                        Gain REAL NOT NULL DEFAULT 1.0,
                        Brightness INTEGER NOT NULL DEFAULT 50,
                        Contrast INTEGER NOT NULL DEFAULT 50,
                        LightingCondition TEXT NOT NULL DEFAULT 'Normal',
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        ModifiedAt TEXT NULL
                    )");
                    
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250308180000_AddCameraSettings', '7.0.0')");
                    
                    result.AppendLine("Table and migration record created successfully");
                }
                else
                {
                    // Both exist - nothing to do
                    result.AppendLine("\nNo fixes needed - table and migration record both exist");
                }
                
                return Content(result.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing CameraSettings table");
                return Content($"Error fixing CameraSettings table: {ex.Message}");
            }
        }

        public async Task<IActionResult> RecreateTableFromScratch()
        {
            try
            {
                var result = new StringBuilder();
                result.AppendLine("Recreating CameraSettings table from scratch");
                result.AppendLine("----------------------------------------");
                
                // First drop the table if it exists
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS CameraSettings;");
                    result.AppendLine("Dropped existing CameraSettings table");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error dropping table: {ex.Message}");
                }
                
                // Now create the table with the correct schema
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE CameraSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProfileName TEXT NOT NULL,
                        ResolutionWidth INTEGER NOT NULL DEFAULT 1280,
                        ResolutionHeight INTEGER NOT NULL DEFAULT 720,
                        ROI_X INTEGER NOT NULL DEFAULT 0,
                        ROI_Y INTEGER NOT NULL DEFAULT 0,
                        ROI_Width INTEGER NOT NULL DEFAULT 640,
                        ROI_Height INTEGER NOT NULL DEFAULT 480,
                        Exposure INTEGER NOT NULL DEFAULT 100,
                        Gain REAL NOT NULL DEFAULT 1.0,
                        Brightness INTEGER NOT NULL DEFAULT 50,
                        Contrast INTEGER NOT NULL DEFAULT 50,
                        LightingCondition TEXT NOT NULL DEFAULT 'Normal',
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        ModifiedAt TEXT NULL
                    )");
                    result.AppendLine("Created new CameraSettings table");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error creating table: {ex.Message}");
                }
                
                // Add a sample record
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO CameraSettings (
                        ProfileName, 
                        ResolutionWidth, 
                        ResolutionHeight, 
                        ROI_X, 
                        ROI_Y, 
                        ROI_Width, 
                        ROI_Height, 
                        Exposure, 
                        Gain, 
                        Brightness, 
                        Contrast, 
                        LightingCondition, 
                        IsActive, 
                        CreatedAt
                    ) VALUES (
                        'Default Profile', 
                        1280, 
                        720, 
                        0, 
                        0, 
                        640, 
                        480, 
                        100, 
                        1.0, 
                        50, 
                        50, 
                        'Normal', 
                        1, 
                        datetime('now')
                    )");
                    result.AppendLine("Added sample camera setting");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error adding sample data: {ex.Message}");
                }
                
                // Ensure migration record exists
                try
                {
                    // First check if migration record exists
                    bool migrationExists = false;
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20250308180000_AddCameraSettings';";
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                            
                        var count = await command.ExecuteScalarAsync();
                        migrationExists = (long)count > 0;
                    }
                    
                    if (!migrationExists)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250308180000_AddCameraSettings', '7.0.0')");
                        result.AppendLine("Added migration record to __EFMigrationsHistory");
                    }
                    else
                    {
                        result.AppendLine("Migration record already exists");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error handling migration record: {ex.Message}");
                }
                
                return Content(result.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recreating CameraSettings table");
                return Content($"Error recreating CameraSettings table: {ex.Message}");
            }
        }

        public async Task<IActionResult> TestCameraSettingsAccess()
        {
            try
            {
                var result = new StringBuilder();
                result.AppendLine("Testing CameraSettings table access:");
                result.AppendLine("----------------------------------");
                
                // First check if table exists
                bool tableExists = false;
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CameraSettings';";
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    var count = await command.ExecuteScalarAsync();
                    tableExists = (long)count > 0;
                }
                
                result.AppendLine($"Table exists: {tableExists}");
                
                if (tableExists)
                {
                    // Try to count the records
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM CameraSettings;";
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                            
                        var count = await command.ExecuteScalarAsync();
                        result.AppendLine($"Record count: {count}");
                    }
                    
                    // Try to list the first 5 records
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT Id, ProfileName, LightingCondition, IsActive, CreatedAt FROM CameraSettings LIMIT 5;";
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                            
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                result.AppendLine("\nCamera Settings Records:");
                                int recordCount = 0;
                                
                                while (await reader.ReadAsync())
                                {
                                    int id = reader.GetInt32(0);
                                    string name = reader.GetString(1);
                                    string lighting = reader.GetString(2);
                                    bool isActive = reader.GetInt32(3) == 1;
                                    string createdAt = reader.GetString(4);
                                    
                                    result.AppendLine($"[{id}] {name} - {lighting} - {(isActive ? "Active" : "Inactive")} - Created: {createdAt}");
                                    recordCount++;
                                }
                                
                                result.AppendLine($"\nListed {recordCount} records");
                            }
                            else
                            {
                                result.AppendLine("No records found");
                            }
                        }
                    }
                    
                    // Try to access using Entity Framework
                    try 
                    {
                        var efCount = await _context.CameraSettings.CountAsync();
                        result.AppendLine($"\nEntity Framework record count: {efCount}");
                        
                        var firstRecord = await _context.CameraSettings.FirstOrDefaultAsync();
                        if (firstRecord != null)
                        {
                            result.AppendLine($"EF First record: [{firstRecord.Id}] {firstRecord.ProfileName} - {firstRecord.LightingCondition}");
                        }
                        else
                        {
                            result.AppendLine("EF No records found");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"Error accessing via Entity Framework: {ex.Message}");
                    }
                }
                
                return Content(result.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing CameraSettings access");
                return Content($"Error testing CameraSettings access: {ex.Message}");
            }
        }
    }
} 