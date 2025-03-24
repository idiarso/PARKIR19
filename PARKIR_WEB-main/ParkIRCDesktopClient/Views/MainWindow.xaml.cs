using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ParkIRCDesktopClient.Services;

namespace ParkIRCDesktopClient.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;
        private UserControl _currentView;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _apiService = new ApiService();
            _signalRService = new SignalRService(_apiService);
            
            // Show login view first
            ShowLoginView();
            
            // Configure SignalR connection state change handler
            _signalRService.ConnectionStateChanged += (sender, state) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ConnectionStatus.Text = state;
                    
                    if (state == "Connected")
                    {
                        ConnectionStatus.Foreground = Brushes.Green;
                    }
                    else if (state == "Disconnected")
                    {
                        ConnectionStatus.Foreground = Brushes.Red;
                    }
                    else
                    {
                        ConnectionStatus.Foreground = Brushes.Yellow;
                    }
                });
            };
        }
        
        private void ShowLoginView()
        {
            // Create and show login view
            LoginView loginView = new LoginView(_apiService, _signalRService);
            loginView.LoginSuccessful += (sender, args) =>
            {
                // Show entry gate by default after login
                Dispatcher.Invoke(() =>
                {
                    ShowEntryGateView();
                });
            };
            
            MainContent.Content = loginView;
            _currentView = loginView;
            
            // Disable navigation buttons until logged in
            EntryGateButton.IsEnabled = false;
            ExitGateButton.IsEnabled = false;
            GateControlsButton.IsEnabled = false;
            LogoutButton.IsEnabled = false;
        }
        
        private void ShowEntryGateView()
        {
            EntryGateView entryGateView = new EntryGateView(_apiService, _signalRService);
            MainContent.Content = entryGateView;
            _currentView = entryGateView;
            
            // Enable navigation buttons
            EntryGateButton.IsEnabled = true;
            ExitGateButton.IsEnabled = true;
            GateControlsButton.IsEnabled = true;
            LogoutButton.IsEnabled = true;
            
            // Update system status
            SystemStatus.Text = "Ready - Entry Gate";
        }
        
        private void ShowExitGateView()
        {
            ExitGateView exitGateView = new ExitGateView(_apiService, _signalRService);
            MainContent.Content = exitGateView;
            _currentView = exitGateView;
            
            // Update system status
            SystemStatus.Text = "Ready - Exit Gate";
        }
        
        private void ShowGateControlsView()
        {
            GateControlsView gateControlsView = new GateControlsView();
            MainContent.Content = gateControlsView;
            _currentView = gateControlsView;
            
            // Update system status
            SystemStatus.Text = "Ready - Gate Controls";
        }

        private void EntryGateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEntryGateView();
        }

        private void ExitGateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowExitGateView();
        }
        
        private void GateControlsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGateControlsView();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Disconnect from SignalR hub
            _signalRService.Disconnect();
            
            // Show login view
            ShowLoginView();
        }
    }
} 