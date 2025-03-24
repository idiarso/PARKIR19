using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParkIRCDesktopClient.Services
{
    public class ImageUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadEndpoint;

        public ImageUploadService(HttpClient httpClient, string uploadEndpoint = "api/v1/image/upload")
        {
            _httpClient = httpClient;
            _uploadEndpoint = uploadEndpoint;
        }

        public async Task<ImageUploadResult> UploadImageAsync(string imagePath, string vehicleNumber, string transactionType)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException("Image file not found", imagePath);
                }

                // Create multipart form content
                using var content = new MultipartFormDataContent();
                
                // Add the image file
                using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                using var imageContent = new StreamContent(fileStream);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(imageContent, "image", Path.GetFileName(imagePath));
                
                // Add other data
                content.Add(new StringContent(vehicleNumber), "vehicleNumber");
                content.Add(new StringContent(transactionType), "transactionType");
                
                // Send the request
                var response = await _httpClient.PostAsync(_uploadEndpoint, content);
                response.EnsureSuccessStatusCode();

                // Parse response
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImageUploadResult>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return new ImageUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ImageUploadResult> UploadEntryImageAsync(string imagePath, string vehicleNumber)
        {
            return await UploadImageAsync(imagePath, vehicleNumber, "entry");
        }

        public async Task<ImageUploadResult> UploadExitImageAsync(string imagePath, string vehicleNumber)
        {
            return await UploadImageAsync(imagePath, vehicleNumber, "exit");
        }
    }

    public class ImageUploadResult
    {
        public bool Success { get; set; }
        public string ImageUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
} 