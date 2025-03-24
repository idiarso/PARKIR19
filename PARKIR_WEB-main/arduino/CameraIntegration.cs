using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace ParkIRCDesktopClient.Services
{
    /// <summary>
    /// Service for managing camera integration with the parking system
    /// </summary>
    public class CameraService : IDisposable
    {
        private FilterInfoCollection _filterInfoCollection;
        private VideoCaptureDevice _videoDevice;
        private string _savePath;
        private bool _isInitialized = false;
        private ManualResetEvent _frameReceivedEvent = new ManualResetEvent(false);
        private Bitmap _currentFrame;
        
        // Events
        public event EventHandler<CameraEventArgs> ImageCaptured;
        public event EventHandler<CameraEventArgs> CameraError;
        
        /// <summary>
        /// Initialize the camera service with a specific save directory path
        /// </summary>
        /// <param name="saveDirectoryPath">Directory to save captured images</param>
        public CameraService(string saveDirectoryPath = null)
        {
            // Set default save path to "captures" in application directory if not specified
            _savePath = saveDirectoryPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");
            
            // Ensure directory exists
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }
        
        /// <summary>
        /// Initialize the camera with a specific device index
        /// </summary>
        /// <param name="cameraIndex">Camera device index (default 0 for first camera)</param>
        /// <returns>Success status</returns>
        public bool Initialize(int cameraIndex = 0)
        {
            try
            {
                // Get available video devices
                _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (_filterInfoCollection.Count == 0)
                {
                    OnCameraError("No camera devices found");
                    return false;
                }
                
                if (cameraIndex >= _filterInfoCollection.Count)
                {
                    OnCameraError($"Camera index {cameraIndex} is out of range. Only {_filterInfoCollection.Count} cameras available.");
                    return false;
                }
                
                // Create video device
                _videoDevice = new VideoCaptureDevice(_filterInfoCollection[cameraIndex].MonikerString);
                
                // Subscribe to new frame event
                _videoDevice.NewFrame += VideoDevice_NewFrame;
                
                // Start the camera
                _videoDevice.Start();
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                OnCameraError($"Error initializing camera: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get a list of available cameras
        /// </summary>
        /// <returns>Array of camera names</returns>
        public string[] GetAvailableCameras()
        {
            try
            {
                _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                string[] cameraNames = new string[_filterInfoCollection.Count];
                
                for (int i = 0; i < _filterInfoCollection.Count; i++)
                {
                    cameraNames[i] = _filterInfoCollection[i].Name;
                }
                
                return cameraNames;
            }
            catch (Exception ex)
            {
                OnCameraError($"Error getting camera list: {ex.Message}");
                return new string[0];
            }
        }
        
        /// <summary>
        /// Handle new video frame events
        /// </summary>
        private void VideoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Create a copy of the frame to work with
            _currentFrame = (Bitmap)eventArgs.Frame.Clone();
            _frameReceivedEvent.Set();
        }
        
        /// <summary>
        /// Capture an image from the camera
        /// </summary>
        /// <param name="licensePlate">Optional license plate text to include in filename</param>
        /// <returns>Path to the saved image, or null if capture failed</returns>
        public async Task<string> CaptureImageAsync(string licensePlate = null)
        {
            if (!_isInitialized || _videoDevice == null)
            {
                OnCameraError("Camera not initialized");
                return null;
            }
            
            try
            {
                // Reset the frame received event
                _frameReceivedEvent.Reset();
                
                // Wait for a frame with timeout
                bool frameReceived = await Task.Run(() => _frameReceivedEvent.WaitOne(5000));
                
                if (!frameReceived)
                {
                    OnCameraError("Timeout waiting for camera frame");
                    return null;
                }
                
                // Create filename with timestamp and optional license plate
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = string.IsNullOrEmpty(licensePlate)
                    ? $"vehicle_{timestamp}.jpg"
                    : $"vehicle_{licensePlate}_{timestamp}.jpg";
                    
                string filePath = Path.Combine(_savePath, filename);
                
                // Save the image
                _currentFrame.Save(filePath, ImageFormat.Jpeg);
                
                // Raise event
                OnImageCaptured(filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                OnCameraError($"Error capturing image: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Handle vehicle detection events from Arduino
        /// </summary>
        /// <param name="messageFromArduino">Message from Arduino serial port</param>
        /// <returns>Path to image if vehicle detected and captured</returns>
        public async Task<string> HandleArduinoMessageAsync(string messageFromArduino)
        {
            // Check if this is a vehicle detection message
            if (messageFromArduino == "VEHICLE_DETECTED:ENTRY")
            {
                // Vehicle detected at entry - capture image
                return await CaptureImageAsync();
            }
            
            return null;
        }
        
        /// <summary>
        /// Raise the ImageCaptured event
        /// </summary>
        protected virtual void OnImageCaptured(string imagePath)
        {
            ImageCaptured?.Invoke(this, new CameraEventArgs
            {
                Success = true,
                ImagePath = imagePath,
                Message = "Image captured successfully"
            });
        }
        
        /// <summary>
        /// Raise the CameraError event
        /// </summary>
        protected virtual void OnCameraError(string errorMessage)
        {
            CameraError?.Invoke(this, new CameraEventArgs
            {
                Success = false,
                Message = errorMessage
            });
        }
        
        /// <summary>
        /// Dispose of camera resources
        /// </summary>
        public void Dispose()
        {
            if (_videoDevice != null && _videoDevice.IsRunning)
            {
                _videoDevice.SignalToStop();
                _videoDevice.WaitForStop();
                _videoDevice.NewFrame -= VideoDevice_NewFrame;
                _videoDevice = null;
            }
            
            if (_currentFrame != null)
            {
                _currentFrame.Dispose();
                _currentFrame = null;
            }
            
            _frameReceivedEvent.Dispose();
        }
    }
    
    /// <summary>
    /// Event arguments for camera events
    /// </summary>
    public class CameraEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ImagePath { get; set; }
    }
}

/*
 * INTEGRATION EXAMPLE:
 * 
 * This shows how to integrate the camera with the Arduino serial events:
 *
 * -----------------------------------------------------
 * 
 * // In your EntryGateView.xaml.cs or relevant code file:
 * 
 * private CameraService _cameraService;
 * 
 * public EntryGateView()
 * {
 *     InitializeComponent();
 *     
 *     // Initialize camera service
 *     _cameraService = new CameraService();
 *     bool cameraInitialized = _cameraService.Initialize();
 *     
 *     if (!cameraInitialized)
 *     {
 *         MessageBox.Show("Could not initialize camera. Please check connections.");
 *     }
 *     
 *     // Subscribe to camera events
 *     _cameraService.ImageCaptured += CameraService_ImageCaptured;
 *     _cameraService.CameraError += CameraService_CameraError;
 *     
 *     // Hook into serial service events
 *     _serialService.EntryGateMessageReceived += SerialService_EntryGateMessageReceived;
 * }
 * 
 * private async void SerialService_EntryGateMessageReceived(object sender, string message)
 * {
 *     // Process the message
 *     LogMessage($"Received: {message}");
 *     
 *     // If vehicle detected, capture image with camera
 *     if (message == "VEHICLE_DETECTED:ENTRY")
 *     {
 *         await _cameraService.CaptureImageAsync();
 *     }
 * }
 * 
 * private void CameraService_ImageCaptured(object sender, CameraEventArgs e)
 * {
 *     // Display or process the captured image
 *     LogMessage($"Image captured: {e.ImagePath}");
 *     
 *     // You could update UI or save the image path to database here
 *     DisplayCapturedImage(e.ImagePath);
 * }
 * 
 * private void CameraService_CameraError(object sender, CameraEventArgs e)
 * {
 *     LogMessage($"Camera error: {e.Message}");
 * }
 * 
 * private void DisplayCapturedImage(string imagePath)
 * {
 *     // Display the captured image in your UI
 *     BitmapImage bitmap = new BitmapImage();
 *     bitmap.BeginInit();
 *     bitmap.UriSource = new Uri(imagePath);
 *     bitmap.CacheOption = BitmapCacheOption.OnLoad;
 *     bitmap.EndInit();
 *     
 *     // Update image control
 *     CapturedImageView.Source = bitmap;
 * }
 * 
 * protected override void OnClosed(EventArgs e)
 * {
 *     // Clean up resources
 *     _cameraService.Dispose();
 *     base.OnClosed(e);
 * }
 */ 