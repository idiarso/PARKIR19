using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ParkIRCDesktopClient.Models;
using ParkIRCDesktopClient.Services;

namespace ParkIRCDesktopClient.Views
{
    public partial class EntryGateView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;
        private readonly CameraService _cameraService;
        private readonly ArduinoService _arduinoService;
        private readonly ImageUploadService _imageUploadService;
        
        private string _capturedImagePath;
        private bool _vehicleDetectedByArduino = false;
        
        // Arduino port settings
        private const string ARDUINO_PORT = "COM3"; // Adjust for your system
        private const int ARDUINO_BAUD_RATE = 9600;
        
        // Camera settings
        private const int CAMERA_ID = 0; // Default camera
        private const string IMAGE_STORAGE_PATH = "Images";

        public EntryGateView(ApiService apiService, SignalRService signalRService)
        {
            InitializeComponent();
            
            _apiService = apiService;
            _signalRService = signalRService;
            
            // Initialize camera service
            _cameraService = new CameraService(CAMERA_ID, IMAGE_STORAGE_PATH);
            _cameraService.FrameCaptured += OnFrameCaptured;
            _cameraService.ImageSaved += OnImageSaved;
            _cameraService.CameraError += OnCameraError;
            
            // Initialize Arduino service
            _arduinoService = new ArduinoService(ARDUINO_PORT, ARDUINO_BAUD_RATE);
            _arduinoService.VehicleDetected += OnVehicleDetected;
            _arduinoService.ButtonPressed += OnButtonPressed;
            _arduinoService.GateStatusChanged += OnGateStatusChanged;
            
            // Initialize image upload service with the API client's HttpClient
            _imageUploadService = new ImageUploadService(_apiService.GetHttpClient());
            
            // Update UI based on initial status
            UpdateArduinoStatusUI(false);
            UpdateGateStatusUI("CLOSED");
            UpdateVehicleDetectionUI(false);
        }

        #region Camera Methods
        
        private void ToggleCameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cameraService.IsRunning)
            {
                _cameraService.StopCamera();
                ToggleCameraButton.Content = "Start Camera";
                CameraStatus.Text = "Camera: Disconnected";
                CameraPreview.Source = null;
            }
            else
            {
                bool success = _cameraService.StartCamera();
                ToggleCameraButton.Content = success ? "Stop Camera" : "Start Camera";
                CameraStatus.Text = success ? "Camera: Connected" : "Camera: Failed to connect";
            }
        }
        
        private void OnFrameCaptured(object sender, BitmapSource frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CameraPreview.Source = frame;
            });
        }
        
        private void OnImageSaved(object sender, string imagePath)
        {
            _capturedImagePath = imagePath;
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Display captured image
                CapturedImage.Source = new BitmapImage(new Uri(imagePath));
            });
        }
        
        private void OnCameraError(object sender, Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Camera error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CameraStatus.Text = "Camera: Error";
            });
        }
        
        #endregion
        
        #region Arduino Methods
        
        private void ConnectArduinoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_arduinoService.IsConnected)
            {
                _arduinoService.Disconnect();
                UpdateArduinoStatusUI(false);
                ConnectArduinoButton.Content = "Connect Arduino";
                OpenGateButton.IsEnabled = false;
                CloseGateButton.IsEnabled = false;
            }
            else
            {
                bool success = _arduinoService.Connect();
                UpdateArduinoStatusUI(success);
                ConnectArduinoButton.Content = success ? "Disconnect Arduino" : "Connect Arduino";
                OpenGateButton.IsEnabled = success;
                CloseGateButton.IsEnabled = success;
            }
        }
        
        private void OpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _arduinoService.OpenGate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open gate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CloseGateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _arduinoService.CloseGate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to close gate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnVehicleDetected(object sender, string location)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                // Only handle ENTRY events here
                if (location != "ENTRY") return;
                
                _vehicleDetectedByArduino = true;
                UpdateVehicleDetectionUI(true);
                
                if (_cameraService.IsRunning)
                {
                    // Automatically capture image when vehicle is detected
                    await Task.Delay(500); // Give time for vehicle to be positioned correctly
                    _capturedImagePath = _cameraService.CaptureImage("auto_entry");
                    
                    // Auto open gate if we have auto mode set
                    _arduinoService.OpenGate();
                }
            });
        }
        
        private void OnButtonPressed(object sender, string location)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Only handle ENTRY events here
                if (location != "ENTRY") return;
                
                // If button is pressed manually, open the gate
                if (_arduinoService.IsConnected)
                {
                    _arduinoService.OpenGate();
                }
            });
        }
        
        private void OnGateStatusChanged(object sender, string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateGateStatusUI(status);
                
                // If vehicle was detected and gate has been opened and then closed, process the entry
                if (_vehicleDetectedByArduino && status == "CLOSED" && !string.IsNullOrEmpty(_capturedImagePath))
                {
                    // This is a trigger to suggest a vehicle has passed through
                    // We don't automatically process entry as we need a plate number
                    // but we can update UI to suggest user needs to enter plate info
                    PlateNumberTextBox.BorderBrush = Brushes.Orange;
                    PlateNumberTextBox.BorderThickness = new Thickness(2);
                    
                    _vehicleDetectedByArduino = false;
                    UpdateVehicleDetectionUI(false);
                }
            });
        }
        
        private void UpdateArduinoStatusUI(bool connected)
        {
            ArduinoStatusIndicator.Fill = connected ? Brushes.Green : Brushes.Red;
            ArduinoStatus.Text = connected ? "Connected" : "Disconnected";
        }
        
        private void UpdateGateStatusUI(string status)
        {
            bool isOpen = status == "OPENED";
            GateStatusIndicator.Fill = isOpen ? Brushes.Green : Brushes.Red;
            GateStatus.Text = isOpen ? "Open" : "Closed";
        }
        
        private void UpdateVehicleDetectionUI(bool detected)
        {
            VehicleSensorIndicator.Fill = detected ? Brushes.Green : Brushes.Gray;
            VehicleDetectionStatus.Text = detected ? "Vehicle Detected" : "No Vehicle";
        }
        
        #endregion
        
        #region Entry Processing
        
        private async void ProcessEntryButton_Click(object sender, RoutedEventArgs e)
        {
            string vehicleNumber = PlateNumberTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(vehicleNumber))
            {
                MessageBox.Show("Please enter a license plate number.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Disable button during processing
            ProcessEntryButton.IsEnabled = false;
            
            try
            {
                // If no image has been captured yet, but camera is running, capture one now
                if (string.IsNullOrEmpty(_capturedImagePath) && _cameraService.IsRunning)
                {
                    _capturedImagePath = _cameraService.CaptureEntryVehicle(vehicleNumber);
                }
                
                // Get selected vehicle type
                var selectedVehicleType = (VehicleTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                
                // Create entry model
                var entryModel = new VehicleEntryModel
                {
                    VehicleNumber = vehicleNumber,
                    VehicleType = selectedVehicleType ?? "Car"
                };
                
                // Record entry in API
                var result = await _apiService.RecordVehicleEntryAsync(entryModel);
                
                // If we have an image, upload it
                if (!string.IsNullOrEmpty(_capturedImagePath) && File.Exists(_capturedImagePath))
                {
                    var uploadResult = await _imageUploadService.UploadEntryImageAsync(_capturedImagePath, vehicleNumber);
                    
                    if (!uploadResult.Success)
                    {
                        Console.WriteLine($"Warning: Failed to upload image: {uploadResult.ErrorMessage}");
                    }
                }
                
                // Show result in UI
                EntryResultPanel.Visibility = Visibility.Visible;
                ResultVehicleNumber.Text = $"Vehicle: {result.VehicleNumber}";
                ResultTicketNumber.Text = $"Ticket #: {result.TicketNumber}";
                ResultEntryTime.Text = $"Time: {result.EntryTime}";
                ResultParkingSpace.Text = $"Space: {result.ParkingSpace}";
                
                // Display image if available
                if (!string.IsNullOrEmpty(_capturedImagePath) && File.Exists(_capturedImagePath))
                {
                    CapturedImage.Source = new BitmapImage(new Uri(_capturedImagePath));
                }
                
                // Clear input
                PlateNumberTextBox.Text = "";
                PlateNumberTextBox.BorderBrush = SystemColors.ControlDarkBrush;
                PlateNumberTextBox.BorderThickness = new Thickness(1);
                
                // Open gate if it's not already open and arduino is connected
                if (_arduinoService.IsConnected && GateStatus.Text != "Open")
                {
                    _arduinoService.OpenGate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                ProcessEntryButton.IsEnabled = true;
            }
        }
        
        #endregion
    }
} 