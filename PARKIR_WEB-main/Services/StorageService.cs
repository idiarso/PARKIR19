using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace ParkIRC.Services
{
    public interface IStorageService
    {
        Task<string> SaveImageAsync(string base64Image, string folder);
        Task<bool> DeleteImageAsync(string imagePath);
        Task<string> GetImagePathAsync(string fileName, string folder);
        Task<long> GetStorageUsageAsync();
    }

    public class StorageService : IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StorageService(ILogger<StorageService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> SaveImageAsync(string base64Image, string folder)
        {
            try
            {
                if (string.IsNullOrEmpty(base64Image) || !base64Image.Contains(","))
                {
                    _logger.LogWarning("Invalid base64 image format");
                    return string.Empty;
                }

                var base64Data = base64Image.Split(',')[1];
                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.jpg";
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(filePath, bytes);

                return $"/uploads/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image");
                return string.Empty;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    return false;
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image");
                return false;
            }
        }

        public Task<string> GetImagePathAsync(string fileName, string folder)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folder))
            {
                return Task.FromResult(string.Empty);
            }

            return Task.FromResult($"/uploads/{folder}/{fileName}");
        }

        public async Task<long> GetStorageUsageAsync()
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    return 0;
                }

                var directoryInfo = new DirectoryInfo(uploadsFolder);
                return await Task.Run(() => GetDirectorySize(directoryInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage usage");
                return 0;
            }
        }

        private long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            long size = 0;
            var files = directoryInfo.GetFiles();
            foreach (var file in files)
            {
                size += file.Length;
            }

            var subDirectories = directoryInfo.GetDirectories();
            foreach (var subDirectory in subDirectories)
            {
                size += GetDirectorySize(subDirectory);
            }

            return size;
        }
    }
} 