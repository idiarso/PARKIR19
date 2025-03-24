using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ParkIRCDesktopClient.Services;

namespace ParkIRCDesktopClient.Views
{
    public partial class GateControlsView : UserControl
    {
        private readonly SerialService _serialService;
        private readonly DispatcherTimer _refreshPortsTimer;
        
        // Maximum log entries to keep
        private const int MaxLogEntries = 100;
        
        // Log tracking
        private List<string> _entryLogEntries = new List<string>();
        private List<string> _exitLogEntries = new List<string>();
        
        public GateControlsView()
        {
            InitializeComponent();
            
            // Initialize the serial service
            _serialService = new SerialService();
            
            // Set up event handlers
            _serialService.EntryGateMessageReceived += EntryGateMessageReceived;
            _serialService.ExitGateMessageReceived += ExitGateMessageReceived;
            _serialService.EntryGateConnectionStatusChanged += EntryGateConnectionStatusChanged;
            _serialService.ExitGateConnectionStatusChanged += ExitGateConnectionStatusChanged;
            
            // Set up port refresh timer
            _refreshPortsTimer = new DispatcherTimer();
            _refreshPortsTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshPortsTimer.Tick += RefreshPortsTimer_Tick;
            _refreshPortsTimer.Start();
            
            // Initial port list load
            RefreshAvailablePorts();
            
            // Set initial UI state
            UpdateEntryUIState(false);
            UpdateExitUIState(false);
        }
        
        private void RefreshPortsTimer_Tick(object sender, EventArgs e)
        {
            RefreshAvailablePorts();
        }
        
        private void RefreshAvailablePorts()
        {
            // Remember selected ports
            string selectedEntryPort = EntryPortComboBox.SelectedItem as string;
            string selectedExitPort = ExitPortComboBox.SelectedItem as string;
            
            // Get available ports
            List<string> ports = _serialService.GetAvailablePorts();
            
            // Update combo boxes
            EntryPortComboBox.ItemsSource = ports;
            ExitPortComboBox.ItemsSource = ports;
            
            // Restore selections if possible
            if (!string.IsNullOrEmpty(selectedEntryPort) && ports.Contains(selectedEntryPort))
            {
                EntryPortComboBox.SelectedItem = selectedEntryPort;
            }
            else if (EntryPortComboBox.Items.Count > 0)
            {
                EntryPortComboBox.SelectedIndex = 0;
            }
            
            if (!string.IsNullOrEmpty(selectedExitPort) && ports.Contains(selectedExitPort))
            {
                ExitPortComboBox.SelectedItem = selectedExitPort;
            }
            else if (ExitPortComboBox.Items.Count > 0)
            {
                ExitPortComboBox.SelectedIndex = 0;
            }
        }
        
        #region Entry Gate Event Handlers
        
        private void EntryConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.IsEntryGateConnected)
            {
                // Disconnect
                _serialService.DisconnectEntryGate();
                AddEntryLogEntry("Disconnected from entry gate");
                UpdateEntryUIState(false);
            }
            else
            {
                // Connect
                string selectedPort = EntryPortComboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedPort))
                {
                    MessageBox.Show("Please select a port.", "Port Required", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                bool connected = _serialService.ConnectToEntryGate(selectedPort);
                if (connected)
                {
                    AddEntryLogEntry($"Connected to entry gate on port {selectedPort}");
                    UpdateEntryUIState(true);
                    // Request initial status
                    EntryStatusButton_Click(sender, e);
                }
            }
        }
        
        private void EntryOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToEntryGate("OPEN_GATE"))
            {
                AddEntryLogEntry("Command sent: Open gate");
            }
        }
        
        private void EntryCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToEntryGate("CLOSE_GATE"))
            {
                AddEntryLogEntry("Command sent: Close gate");
            }
        }
        
        private void EntryStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToEntryGate("STATUS"))
            {
                AddEntryLogEntry("Command sent: Get status");
            }
        }
        
        private void EntryGateMessageReceived(object sender, string message)
        {
            AddEntryLogEntry($"Received: {message}");
            
            // Parse message to update UI
            if (message.StartsWith("STATUS:"))
            {
                string[] parts = message.Substring(7).Split(',');
                if (parts.Length >= 2)
                {
                    UpdateEntryGateState(parts[0]);
                    UpdateEntrySensorState(parts[1]);
                }
            }
            else if (message == "GATE_OPENED")
            {
                UpdateEntryGateState("OPEN");
            }
            else if (message == "GATE_CLOSED")
            {
                UpdateEntryGateState("CLOSED");
            }
            else if (message == "VEHICLE_DETECTED:ENTRY")
            {
                UpdateEntrySensorState("VEHICLE_DETECTED");
            }
            else if (message == "VEHICLE_LEFT:ENTRY")
            {
                UpdateEntrySensorState("NO_VEHICLE");
            }
        }
        
        private void EntryGateConnectionStatusChanged(object sender, string status)
        {
            EntryStatusTextBlock.Text = status;
            UpdateEntryUIState(_serialService.IsEntryGateConnected);
        }
        
        private void UpdateEntryGateState(string state)
        {
            EntryGateStateTextBlock.Text = state;
        }
        
        private void UpdateEntrySensorState(string state)
        {
            EntrySensorStateTextBlock.Text = state;
        }
        
        private void UpdateEntryUIState(bool connected)
        {
            EntryConnectButton.Content = connected ? "Disconnect" : "Connect";
            EntryOpenButton.IsEnabled = connected;
            EntryCloseButton.IsEnabled = connected;
            EntryStatusButton.IsEnabled = connected;
            EntryPortComboBox.IsEnabled = !connected;
        }
        
        private void AddEntryLogEntry(string entry)
        {
            _entryLogEntries.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {entry}");
            
            // Trim log if too large
            if (_entryLogEntries.Count > MaxLogEntries)
            {
                _entryLogEntries.RemoveAt(0);
            }
            
            // Update the log text
            EntryLogTextBlock.Text = string.Join(Environment.NewLine, _entryLogEntries);
            
            // Scroll to end
            if (EntryLogTextBlock.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToEnd();
            }
        }
        
        #endregion
        
        #region Exit Gate Event Handlers
        
        private void ExitConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.IsExitGateConnected)
            {
                // Disconnect
                _serialService.DisconnectExitGate();
                AddExitLogEntry("Disconnected from exit gate");
                UpdateExitUIState(false);
            }
            else
            {
                // Connect
                string selectedPort = ExitPortComboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedPort))
                {
                    MessageBox.Show("Please select a port.", "Port Required", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                bool connected = _serialService.ConnectToExitGate(selectedPort);
                if (connected)
                {
                    AddExitLogEntry($"Connected to exit gate on port {selectedPort}");
                    UpdateExitUIState(true);
                    // Request initial status
                    ExitStatusButton_Click(sender, e);
                }
            }
        }
        
        private void ExitOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToExitGate("OPEN_GATE"))
            {
                AddExitLogEntry("Command sent: Open gate");
            }
        }
        
        private void ExitCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToExitGate("CLOSE_GATE"))
            {
                AddExitLogEntry("Command sent: Close gate");
            }
        }
        
        private void ExitStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialService.SendCommandToExitGate("STATUS"))
            {
                AddExitLogEntry("Command sent: Get status");
            }
        }
        
        private void ExitGateMessageReceived(object sender, string message)
        {
            AddExitLogEntry($"Received: {message}");
            
            // Parse message to update UI
            if (message.StartsWith("STATUS:"))
            {
                string[] parts = message.Substring(7).Split(',');
                if (parts.Length >= 2)
                {
                    UpdateExitGateState(parts[0]);
                    UpdateExitSensorState(parts[1]);
                }
            }
            else if (message == "GATE_OPENED")
            {
                UpdateExitGateState("OPEN");
            }
            else if (message == "GATE_CLOSED")
            {
                UpdateExitGateState("CLOSED");
            }
            else if (message == "VEHICLE_DETECTED:EXIT")
            {
                UpdateExitSensorState("VEHICLE_DETECTED");
            }
            else if (message == "VEHICLE_LEFT:EXIT")
            {
                UpdateExitSensorState("NO_VEHICLE");
            }
        }
        
        private void ExitGateConnectionStatusChanged(object sender, string status)
        {
            ExitStatusTextBlock.Text = status;
            UpdateExitUIState(_serialService.IsExitGateConnected);
        }
        
        private void UpdateExitGateState(string state)
        {
            ExitGateStateTextBlock.Text = state;
        }
        
        private void UpdateExitSensorState(string state)
        {
            ExitSensorStateTextBlock.Text = state;
        }
        
        private void UpdateExitUIState(bool connected)
        {
            ExitConnectButton.Content = connected ? "Disconnect" : "Connect";
            ExitOpenButton.IsEnabled = connected;
            ExitCloseButton.IsEnabled = connected;
            ExitStatusButton.IsEnabled = connected;
            ExitPortComboBox.IsEnabled = !connected;
        }
        
        private void AddExitLogEntry(string entry)
        {
            _exitLogEntries.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {entry}");
            
            // Trim log if too large
            if (_exitLogEntries.Count > MaxLogEntries)
            {
                _exitLogEntries.RemoveAt(0);
            }
            
            // Update the log text
            ExitLogTextBlock.Text = string.Join(Environment.NewLine, _exitLogEntries);
            
            // Scroll to end
            if (ExitLogTextBlock.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToEnd();
            }
        }
        
        #endregion
    }
} 