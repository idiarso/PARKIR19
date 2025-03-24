using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace ParkIRCDesktopClient.Services
{
    public class CameraService : IDisposable
    {
        private VideoCapture _capture;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private readonly int _cameraId;
        private readonly string _storagePath;
        private readonly object _lockObject = new object();

        public event EventHandler<BitmapSource> FrameCaptured;
        public event EventHandler<string> ImageSaved;
        public event EventHandler<Exception> CameraError;

        public CameraService(int cameraId = 0, string storagePath = "Images")
        {
            _cameraId = cameraId;
            _storagePath = storagePath;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Buat direktori penyimpanan jika belum ada
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public bool IsRunning => _isRunning;

        public bool StartCamera()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isRunning)
                        return true;

                    _capture = new VideoCapture(_cameraId);
                    if (!_capture.IsOpened())
                    {
                        throw new Exception($"Could not open camera {_cameraId}");
                    }

                    _cancellationTokenSource = new CancellationTokenSource();
                    _isRunning = true;

                    // Start camera capture loop in a separate thread
                    Task.Run(() => CaptureLoop(_cancellationTokenSource.Token));

                    return true;
                }
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex);
                _isRunning = false;
                return false;
            }
        }

        public void StopCamera()
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_isRunning)
                        return;

                    _cancellationTokenSource.Cancel();
                    _isRunning = false;

                    if (_capture != null)
                    {
                        _capture.Dispose();
                        _capture = null;
                    }
                }
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex);
            }
        }

        // Mengambil gambar dari kamera
        public string CaptureImage(string prefix = "vehicle")
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_isRunning || _capture == null)
                    {
                        throw new InvalidOperationException("Camera is not running");
                    }

                    using var frame = new Mat();
                    _capture.Read(frame);

                    if (frame.Empty())
                    {
                        throw new Exception("Failed to capture image, frame is empty");
                    }

                    // Menggunakan timestamp dalam nama file
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"{prefix}_{timestamp}.jpg";
                    string filePath = Path.Combine(_storagePath, fileName);

                    // Simpan gambar
                    frame.SaveImage(filePath);

                    // Notifikasi bahwa gambar telah disimpan
                    ImageSaved?.Invoke(this, filePath);

                    return filePath;
                }
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex);
                return null;
            }
        }

        // Mengambil gambar entry kendaraan
        public string CaptureEntryVehicle(string vehicleNumber)
        {
            return CaptureImage($"entry_{vehicleNumber.Replace(" ", "_")}");
        }

        // Mengambil gambar exit kendaraan
        public string CaptureExitVehicle(string vehicleNumber)
        {
            return CaptureImage($"exit_{vehicleNumber.Replace(" ", "_")}");
        }

        // Camera loop untuk live preview
        private async Task CaptureLoop(CancellationToken token)
        {
            try
            {
                using var frame = new Mat();

                while (!token.IsCancellationRequested && _isRunning)
                {
                    if (_capture != null && _capture.IsOpened())
                    {
                        _capture.Read(frame);

                        if (!frame.Empty())
                        {
                            // Convert to WPF compatible format
                            var bitmap = frame.ToBitmapSource();
                            
                            // Fire event with the frame
                            FrameCaptured?.Invoke(this, bitmap);
                        }
                    }

                    // Small delay to prevent CPU hogging
                    await Task.Delay(33, token); // ~30 FPS
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, just exit
            }
            catch (Exception ex)
            {
                CameraError?.Invoke(this, ex);
            }
        }

        public void Dispose()
        {
            StopCamera();
            _cancellationTokenSource.Dispose();
        }
    }
} 