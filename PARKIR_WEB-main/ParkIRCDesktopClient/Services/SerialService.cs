using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;

namespace ParkIRCDesktopClient.Services
{
    public class SerialService
    {
        private SerialPort _entryGatePort;
        private SerialPort _exitGatePort;
        private bool _isEntryGateConnected;
        private bool _isExitGateConnected;
        
        // Event handlers for serial events
        public event EventHandler<string> EntryGateMessageReceived;
        public event EventHandler<string> ExitGateMessageReceived;
        public event EventHandler<string> EntryGateConnectionStatusChanged;
        public event EventHandler<string> ExitGateConnectionStatusChanged;
        
        public SerialService()
        {
            _isEntryGateConnected = false;
            _isExitGateConnected = false;
        }
        
        public List<string> GetAvailablePorts()
        {
            return new List<string>(SerialPort.GetPortNames());
        }
        
        public bool ConnectToEntryGate(string portName, int baudRate = 9600)
        {
            try
            {
                _entryGatePort = new SerialPort(portName, baudRate);
                _entryGatePort.DataReceived += EntryGateDataReceived;
                _entryGatePort.Open();
                _isEntryGateConnected = true;
                EntryGateConnectionStatusChanged?.Invoke(this, "Connected");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to entry gate: {ex.Message}", "Connection Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                _isEntryGateConnected = false;
                EntryGateConnectionStatusChanged?.Invoke(this, $"Error: {ex.Message}");
                return false;
            }
        }
        
        public bool ConnectToExitGate(string portName, int baudRate = 9600)
        {
            try
            {
                _exitGatePort = new SerialPort(portName, baudRate);
                _exitGatePort.DataReceived += ExitGateDataReceived;
                _exitGatePort.Open();
                _isExitGateConnected = true;
                ExitGateConnectionStatusChanged?.Invoke(this, "Connected");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to exit gate: {ex.Message}", "Connection Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                _isExitGateConnected = false;
                ExitGateConnectionStatusChanged?.Invoke(this, $"Error: {ex.Message}");
                return false;
            }
        }
        
        private void EntryGateDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string data = sp.ReadLine().Trim();
                
                if (!string.IsNullOrEmpty(data))
                {
                    // Invoke the event on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EntryGateMessageReceived?.Invoke(this, data);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error reading from entry gate: {ex.Message}", "Serial Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
        
        private void ExitGateDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string data = sp.ReadLine().Trim();
                
                if (!string.IsNullOrEmpty(data))
                {
                    // Invoke the event on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExitGateMessageReceived?.Invoke(this, data);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error reading from exit gate: {ex.Message}", "Serial Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
        
        public bool SendCommandToEntryGate(string command)
        {
            if (!_isEntryGateConnected || _entryGatePort == null || !_entryGatePort.IsOpen)
            {
                MessageBox.Show("Entry gate is not connected.", "Connection Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            try
            {
                _entryGatePort.WriteLine(command);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending command to entry gate: {ex.Message}", "Serial Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public bool SendCommandToExitGate(string command)
        {
            if (!_isExitGateConnected || _exitGatePort == null || !_exitGatePort.IsOpen)
            {
                MessageBox.Show("Exit gate is not connected.", "Connection Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            try
            {
                _exitGatePort.WriteLine(command);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending command to exit gate: {ex.Message}", "Serial Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<string> GetEntryGateStatusAsync()
        {
            if (!_isEntryGateConnected || _entryGatePort == null || !_entryGatePort.IsOpen)
            {
                return "Not Connected";
            }
            
            try
            {
                string status = null;
                
                // Set up a one-time event handler
                EventHandler<string> handler = null;
                var tcs = new TaskCompletionSource<string>();
                
                handler = (s, e) =>
                {
                    if (e.StartsWith("STATUS:"))
                    {
                        status = e.Substring(7);  // Remove "STATUS:" prefix
                        EntryGateMessageReceived -= handler;
                        tcs.SetResult(status);
                    }
                };
                
                EntryGateMessageReceived += handler;
                
                // Send the status command
                _entryGatePort.WriteLine("STATUS");
                
                // Add a timeout
                var timeoutTask = Task.Delay(3000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    EntryGateMessageReceived -= handler;
                    return "Timeout";
                }
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting entry gate status: {ex.Message}", "Serial Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return $"Error: {ex.Message}";
            }
        }
        
        public async Task<string> GetExitGateStatusAsync()
        {
            if (!_isExitGateConnected || _exitGatePort == null || !_exitGatePort.IsOpen)
            {
                return "Not Connected";
            }
            
            try
            {
                string status = null;
                
                // Set up a one-time event handler
                EventHandler<string> handler = null;
                var tcs = new TaskCompletionSource<string>();
                
                handler = (s, e) =>
                {
                    if (e.StartsWith("STATUS:"))
                    {
                        status = e.Substring(7);  // Remove "STATUS:" prefix
                        ExitGateMessageReceived -= handler;
                        tcs.SetResult(status);
                    }
                };
                
                ExitGateMessageReceived += handler;
                
                // Send the status command
                _exitGatePort.WriteLine("STATUS");
                
                // Add a timeout
                var timeoutTask = Task.Delay(3000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    ExitGateMessageReceived -= handler;
                    return "Timeout";
                }
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting exit gate status: {ex.Message}", "Serial Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return $"Error: {ex.Message}";
            }
        }
        
        public void DisconnectEntryGate()
        {
            if (_isEntryGateConnected && _entryGatePort != null && _entryGatePort.IsOpen)
            {
                try
                {
                    _entryGatePort.Close();
                    _entryGatePort.Dispose();
                    _isEntryGateConnected = false;
                    EntryGateConnectionStatusChanged?.Invoke(this, "Disconnected");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error disconnecting from entry gate: {ex.Message}", "Serial Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        public void DisconnectExitGate()
        {
            if (_isExitGateConnected && _exitGatePort != null && _exitGatePort.IsOpen)
            {
                try
                {
                    _exitGatePort.Close();
                    _exitGatePort.Dispose();
                    _isExitGateConnected = false;
                    ExitGateConnectionStatusChanged?.Invoke(this, "Disconnected");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error disconnecting from exit gate: {ex.Message}", "Serial Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        public bool IsEntryGateConnected => _isEntryGateConnected;
        public bool IsExitGateConnected => _isExitGateConnected;
    }
} 