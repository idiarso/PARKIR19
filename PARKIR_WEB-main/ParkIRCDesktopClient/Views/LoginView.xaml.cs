using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ParkIRCDesktopClient.Services;

namespace ParkIRCDesktopClient.Views
{
    public partial class LoginView : UserControl
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;
        private readonly MainWindow _mainWindow;
        
        public LoginView(ApiService apiService, SignalRService signalRService, MainWindow mainWindow)
        {
            InitializeComponent();
            
            _apiService = apiService;
            _signalRService = signalRService;
            _mainWindow = mainWindow;
            
            // Set default credentials for demo
            UsernameTextBox.Text = "admin@parkingsystem.com";
            PasswordBox.Password = "Admin@123";
        }
        
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous error
            ErrorMessage.Text = "";
            
            // Get login credentials
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            
            // Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Please enter both username and password.";
                return;
            }
            
            // Show loading
            LoginButton.IsEnabled = false;
            LoadingPanel.Visibility = Visibility.Visible;
            
            try
            {
                // Attempt login
                var result = await _apiService.LoginAsync(username, password);
                
                // Connect to SignalR
                await _signalRService.ConnectAsync(result.Token);
                
                // Notify main window of successful login
                _mainWindow.OnLoginSuccess();
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Login failed: {ex.Message}";
                LoginButton.IsEnabled = true;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
} 