using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Data;
using Microsoft.Data.Sqlite;
using ParkIRC.Models;
using System.Text.Json;

namespace ParkIRC.Services
{
    public class LocalDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<LocalDatabaseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        public LocalDatabaseService(
            ILogger<LocalDatabaseService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("LocalDb");
            _httpClient = httpClientFactory.CreateClient("MainServer");
        }
        
        public async Task<bool> SaveLocalEntry(ParkingTransaction transaction)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Save to local DB first
                    const string sql = @"
                        INSERT INTO local_transactions 
                        (ticket_number, entry_time, vehicle_number, vehicle_type, 
                         entry_point, image_path, operator_id, is_synced)
                        VALUES 
                        (@ticketNumber, @entryTime, @vehicleNumber, @vehicleType,
                         @entryPoint, @imagePath, @operatorId, false)";
                    
                    await connection.ExecuteAsync(sql, new {
                        ticketNumber = transaction.TicketNumber,
                        entryTime = transaction.EntryTime,
                        vehicleNumber = transaction.VehicleNumber,
                        vehicleType = transaction.VehicleType,
                        entryPoint = transaction.EntryPoint,
                        imagePath = transaction.ImagePath,
                        operatorId = transaction.OperatorId
                    });
                    
                    _logger.LogInformation($"Saved local entry: {transaction.TicketNumber}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving local entry");
                return false;
            }
        }
        
        public async Task SyncWithMainServer()
        {
            try
            {
                _logger.LogInformation("Starting sync with main server...");
                
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get unsynchronized data
                    const string sql = @"
                        SELECT * FROM local_transactions 
                        WHERE is_synced = false
                        ORDER BY entry_time";
                        
                    var unsynced = await connection.QueryAsync<OfflineEntry>(sql);
                    
                    if (!unsynced.Any())
                    {
                        _logger.LogInformation("No data to sync");
                        return;
                    }
                    
                    // Sync with main server
                    var syncEndpoint = _configuration["SyncSettings:MainServerUrl"] + "/api/offlinedata/sync";
                    var response = await _httpClient.PostAsJsonAsync(syncEndpoint, unsynced);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Mark all as synced
                        const string updateSql = @"
                            UPDATE local_transactions 
                            SET is_synced = true,
                                sync_time = @syncTime
                            WHERE is_synced = false";
                            
                        await connection.ExecuteAsync(updateSql, new { syncTime = DateTime.Now });
                        _logger.LogInformation($"Successfully synced {unsynced.Count()} entries");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Sync failed: {error}");
                        throw new Exception($"Sync failed: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing with main server");
                throw;
            }
        }
        
        public async Task<IEnumerable<OfflineEntry>> GetPendingEntries()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    const string sql = @"
                        SELECT * FROM local_transactions 
                        WHERE is_synced = false
                        ORDER BY entry_time";
                        
                    return await connection.QueryAsync<OfflineEntry>(sql);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending entries");
                return Enumerable.Empty<OfflineEntry>();
            }
        }
    }
} 