using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ParkIRCDesktopClient.Models;

namespace ParkIRCDesktopClient.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _token;

        public ApiService(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var loginModel = new LoginModel
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/v1/Auth/login", loginModel);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                _token = result.Token;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                return result;
            }
            
            throw new Exception($"Login failed: {response.StatusCode}");
        }

        public async Task<DashboardData> GetDashboardDataAsync()
        {
            EnsureAuthenticated();
            
            var response = await _httpClient.GetAsync("api/v1/Parking/dashboard");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DashboardData>();
            }
            
            throw new Exception($"Failed to get dashboard data: {response.StatusCode}");
        }

        public async Task<EntryResponse> RecordVehicleEntryAsync(VehicleEntryModel model)
        {
            EnsureAuthenticated();
            
            var response = await _httpClient.PostAsJsonAsync("api/v1/Parking/entry", model);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EntryResponse>();
            }
            
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to record vehicle entry: {error}");
        }

        public async Task<ExitResponse> RecordVehicleExitAsync(ExitModel model)
        {
            EnsureAuthenticated();
            
            var response = await _httpClient.PostAsJsonAsync("api/v1/Parking/exit", model);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ExitResponse>();
            }
            
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to record vehicle exit: {error}");
        }

        public HttpClient GetHttpClient()
        {
            // Return the HttpClient instance for use by other services
            return _httpClient;
        }

        private void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
            {
                throw new Exception("Not authenticated. Please login first.");
            }
        }
    }
} 