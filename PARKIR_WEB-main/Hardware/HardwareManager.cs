using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections.Generic;
using System.Configuration;
using AForge.Video;
using AForge.Video.DirectShow;
using Npgsql;

namespace SimpleParkingAdmin.Hardware
{
    /// <summary>
    /// Manages communication with hardware devices:
    /// - Camera (IP camera or webcam)
    /// - Gate controller (via serial port)
    /// - Thermal printer
    /// </summary>
    public class HardwareManager : IDisposable
    {
        private SerialPort _serialPort;
        private VideoCaptureDevice _videoDevice;
        private bool _isInitialized = false;
        private string _basePath;
        private bool _disposed = false;
        private static readonly object _lock = new object();
        private static HardwareManager _instance;

        // Configuration values
        private string _cameraType;
        private string _cameraIp;
        private string _cameraUsername;
        private string _cameraPassword;
        private int _cameraPort;
        private string _cameraResolution;
        private int _webcamDeviceIndex;
        private string _comPort;
        private int _baudRate;
        private string _imageBasePath;
        private string _entrySubfolder;
        private string _exitSubfolder;
        private string _filenameFormat;

        // Camera event delegate
        public delegate void ImageCapturedEventHandler(object sender, ImageCapturedEventArgs e);
        public event ImageCapturedEventHandler ImageCaptured;

        // Serial event delegate
        public delegate void CommandReceivedEventHandler(object sender, CommandReceivedEventArgs e);
        public event CommandReceivedEventHandler CommandReceived;

        // Singleton implementation
        public static HardwareManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new HardwareManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private HardwareManager()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Initializes hardware connections
        /// </summary>
        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                    return true;

                InitializeSerialPort();
                InitializeCamera();
                EnsureDirectoriesExist();
                
                _isInitialized = true;
                Console.WriteLine("Hardware manager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize hardware: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads configuration from INI files
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Load camera settings
                string cameraConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "camera.ini");
                var cameraConfig = ParseIniFile(cameraConfigPath);
                
                _cameraType = GetIniValue(cameraConfig, "Camera", "Type", "Webcam");
                _cameraIp = GetIniValue(cameraConfig, "Camera", "IP", "127.0.0.1");
                _cameraUsername = GetIniValue(cameraConfig, "Camera", "Username", "admin");
                _cameraPassword = GetIniValue(cameraConfig, "Camera", "Password", "admin");
                _cameraPort = int.Parse(GetIniValue(cameraConfig, "Camera", "Port", "8080"));
                _cameraResolution = GetIniValue(cameraConfig, "Camera", "Resolution", "640x480");
                _webcamDeviceIndex = int.Parse(GetIniValue(cameraConfig, "Webcam", "Device_Index", "0"));

                _imageBasePath = GetIniValue(cameraConfig, "Storage", "Base_Path", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images"));
                _entrySubfolder = GetIniValue(cameraConfig, "Storage", "Entry_Subfolder", "Entry");
                _exitSubfolder = GetIniValue(cameraConfig, "Storage", "Exit_Subfolder", "Exit");
                _filenameFormat = GetIniValue(cameraConfig, "Storage", "Filename_Format", "{0}_{1:yyyyMMdd_HHmmss}");

                // Load gate settings
                string gateConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "gate.ini");
                var gateConfig = ParseIniFile(gateConfigPath);
                
                _comPort = GetIniValue(gateConfig, "Gate", "COM_Port", "COM1");
                _baudRate = int.Parse(GetIniValue(gateConfig, "Gate", "Baud_Rate", "9600"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes serial port for communication with gate controller
        /// </summary>
        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort(_comPort, _baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                Console.WriteLine($"Connected to serial port {_comPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize serial port: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes camera device (webcam or IP camera)
        /// </summary>
        private void InitializeCamera()
        {
            try
            {
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    // Initialize local webcam
                    FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    if (videoDevices.Count == 0)
                    {
                        throw new Exception("No video devices found");
                    }

                    int deviceIndex = Math.Min(_webcamDeviceIndex, videoDevices.Count - 1);
                    _videoDevice = new VideoCaptureDevice(videoDevices[deviceIndex].MonikerString);
                    
                    // Find closest resolution
                    string[] dimensions = _cameraResolution.Split('x');
                    int targetWidth = int.Parse(dimensions[0]);
                    int targetHeight = int.Parse(dimensions[1]);
                    
                    bool resolutionSet = false;
                    foreach (VideoCapabilities capability in _videoDevice.VideoCapabilities)
                    {
                        if (capability.FrameSize.Width == targetWidth && capability.FrameSize.Height == targetHeight)
                        {
                            _videoDevice.VideoResolution = capability;
                            resolutionSet = true;
                            break;
                        }
                    }
                    
                    if (!resolutionSet && _videoDevice.VideoCapabilities.Length > 0)
                    {
                        // Select default resolution
                        _videoDevice.VideoResolution = _videoDevice.VideoCapabilities[0];
                    }

                    _videoDevice.NewFrame += VideoDevice_NewFrame;
                    _videoDevice.Start();
                    Console.WriteLine("Webcam initialized");
                }
                else if (_cameraType.Equals("IP", StringComparison.OrdinalIgnoreCase))
                {
                    // For IP camera, we'll use HTTP requests to capture images
                    // This would typically be implemented with a specific SDK or HTTP client
                    Console.WriteLine("IP camera initialized");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize camera: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates necessary directories for image storage
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(_imageBasePath, _entrySubfolder));
                Directory.CreateDirectory(Path.Combine(_imageBasePath, _exitSubfolder));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create directories: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Captures image from camera and saves it with the specified ticket ID
        /// </summary>
        public async Task<string> CaptureEntryImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, true);
        }

        /// <summary>
        /// Captures image for vehicle exit and saves it with the specified ticket ID
        /// </summary>
        public async Task<string> CaptureExitImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, false);
        }

        /// <summary>
        /// Internal method to capture and save image
        /// </summary>
        private async Task<string> CaptureImageAsync(string ticketId, bool isEntry)
        {
            string subfolder = isEntry ? _entrySubfolder : _exitSubfolder;
            string filePath = "";

            try
            {
                // For webcam, use NewFrame event to capture image
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    // Set up a task completion source to get the frame asynchronously
                    var tcs = new TaskCompletionSource<Bitmap>();
                    EventHandler<NewFrameEventArgs> frameHandler = null;
                    
                    frameHandler = (sender, e) =>
                    {
                        // Clone the frame to avoid disposal issues
                        var bitmap = (Bitmap)e.Frame.Clone();
                        tcs.TrySetResult(bitmap);
                        
                        // Remove the event handler after capturing a frame
                        if (_videoDevice != null)
                        {
                            _videoDevice.NewFrame -= frameHandler;
                        }
                    };
                    
                    _videoDevice.NewFrame += frameHandler;
                    
                    // Wait for the frame with a timeout
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException("Timeout waiting for camera frame");
                    }
                    
                    var frame = await tcs.Task;
                    
                    // Generate filename and save
                    string filename = string.Format(_filenameFormat, ticketId, DateTime.Now) + ".jpg";
                    filePath = Path.Combine(_imageBasePath, subfolder, filename);
                    
                    frame.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    
                    // Raise event
                    OnImageCaptured(new ImageCapturedEventArgs(ticketId, filePath, isEntry));
                    
                    // Clean up
                    frame.Dispose();
                }
                else if (_cameraType.Equals("IP", StringComparison.OrdinalIgnoreCase))
                {
                    // For IP camera, implement HTTP snapshot capture
                    // This is a placeholder for IP camera implementation
                    string url = $"http://{_cameraIp}:{_cameraPort}/snapshot.jpg";
                    
                    // Create filename
                    string filename = string.Format(_filenameFormat, ticketId, DateTime.Now) + ".jpg";
                    filePath = Path.Combine(_imageBasePath, subfolder, filename);
                    
                    // Would typically use HttpClient to get image
                    // For now, just log the operation
                    Console.WriteLine($"Would capture IP camera image from {url} and save to {filePath}");
                    
                    // Raise event with mock filepath
                    OnImageCaptured(new ImageCapturedEventArgs(ticketId, filePath, isEntry));
                }
                
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing image: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Opens the entry gate
        /// </summary>
        public bool OpenEntryGate()
        {
            return SendCommand("OPEN_ENTRY");
        }

        /// <summary>
        /// Opens the exit gate
        /// </summary>
        public bool OpenExitGate()
        {
            return SendCommand("OPEN_EXIT");
        }

        /// <summary>
        /// Sends command to the gate controller
        /// </summary>
        private bool SendCommand(string command)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    InitializeSerialPort();
                }

                _serialPort.WriteLine(command);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending command to gate controller: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles data received from serial port
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    string data = _serialPort.ReadLine().Trim();
                    Console.WriteLine($"Received from serial: {data}");
                    
                    // Parse command from serial data
                    if (data.StartsWith("IN:"))
                    {
                        string id = data.Substring(3);
                        OnCommandReceived(new CommandReceivedEventArgs("IN", id));
                    }
                    else if (data.StartsWith("OUT:"))
                    {
                        string id = data.Substring(4);
                        OnCommandReceived(new CommandReceivedEventArgs("OUT", id));
                    }
                    else if (data.StartsWith("STATUS:"))
                    {
                        string status = data.Substring(7);
                        OnCommandReceived(new CommandReceivedEventArgs("STATUS", status));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing serial data: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles video frame captures
        /// </summary>
        private void VideoDevice_NewFrame(object sender, NewFrameEventArgs e)
        {
            // This is used for continuous frame preview
            // For actual image capture we use separate method
        }

        /// <summary>
        /// Raises the ImageCaptured event
        /// </summary>
        protected virtual void OnImageCaptured(ImageCapturedEventArgs e)
        {
            ImageCaptured?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        protected virtual void OnCommandReceived(CommandReceivedEventArgs e)
        {
            CommandReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Helper method to parse INI files
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> ParseIniFile(string path)
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmedLine = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                // Section header
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!iniData.ContainsKey(currentSection))
                    {
                        iniData[currentSection] = new Dictionary<string, string>();
                    }
                }
                // Key-value pair
                else if (trimmedLine.Contains("="))
                {
                    string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2 && !string.IsNullOrEmpty(currentSection))
                    {
                        iniData[currentSection][parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return iniData;
        }

        /// <summary>
        /// Helper method to get value from INI data
        /// </summary>
        private string GetIniValue(Dictionary<string, Dictionary<string, string>> iniData, string section, string key, string defaultValue)
        {
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
            {
                return iniData[section][key];
            }
            return defaultValue;
        }

        /// <summary>
        /// Disposes hardware resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (_videoDevice != null && _videoDevice.IsRunning)
                    {
                        _videoDevice.SignalToStop();
                        _videoDevice.WaitForStop();
                        _videoDevice = null;
                    }

                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                }

                _disposed = true;
            }
        }

        ~HardwareManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Event arguments for image capture events
    /// </summary>
    public class ImageCapturedEventArgs : EventArgs
    {
        public string TicketId { get; }
        public string ImagePath { get; }
        public bool IsEntryImage { get; }

        public ImageCapturedEventArgs(string ticketId, string imagePath, bool isEntryImage)
        {
            TicketId = ticketId;
            ImagePath = imagePath;
            IsEntryImage = isEntryImage;
        }
    }

    /// <summary>
    /// Event arguments for command received events
    /// </summary>
    public class CommandReceivedEventArgs : EventArgs
    {
        public string Command { get; }
        public string Data { get; }

        public CommandReceivedEventArgs(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }
}
