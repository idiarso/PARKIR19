using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace ParkIRCDesktopClient.Services
{
    public class ArduinoService : IDisposable
    {
        private SerialPort _serialPort;
        private bool _isConnected = false;
        private readonly string _portName;
        private readonly int _baudRate;
        private CancellationTokenSource _cancellationTokenSource;

        // Event untuk notifikasi deteksi kendaraan
        public event EventHandler<string> VehicleDetected;
        
        // Event untuk notifikasi push button ditekan
        public event EventHandler<string> ButtonPressed;
        
        // Event untuk notifikasi gate status (open/closed)
        public event EventHandler<string> GateStatusChanged;

        public ArduinoService(string portName, int baudRate = 9600)
        {
            _portName = portName;
            _baudRate = baudRate;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected => _isConnected;

        public bool Connect()
        {
            try
            {
                // Cek jika sudah terhubung
                if (_isConnected)
                    return true;

                // Inisialisasi serial port
                _serialPort = new SerialPort
                {
                    PortName = _portName,
                    BaudRate = _baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                // Buka koneksi
                _serialPort.Open();
                _isConnected = true;

                // Start listening untuk pesan dari Arduino
                Task.Run(() => StartListening(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Arduino: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                
                _isConnected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from Arduino: {ex.Message}");
            }
        }

        // Mengirim perintah ke Arduino
        public void SendCommand(string command)
        {
            try
            {
                if (!_isConnected || _serialPort == null || !_serialPort.IsOpen)
                {
                    throw new InvalidOperationException("Arduino is not connected");
                }

                _serialPort.WriteLine(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending command to Arduino: {ex.Message}");
                throw;
            }
        }

        // Buka gerbang
        public void OpenGate()
        {
            SendCommand("OPEN_GATE");
        }

        // Tutup gerbang
        public void CloseGate()
        {
            SendCommand("CLOSE_GATE");
        }

        // Listening loop untuk pesan dari Arduino
        private async Task StartListening(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isConnected)
            {
                try
                {
                    if (_serialPort != null && _serialPort.IsOpen && _serialPort.BytesToRead > 0)
                    {
                        // Baca pesan dari Arduino
                        string message = _serialPort.ReadLine().Trim();
                        
                        // Parse pesan berdasarkan formatnya
                        ProcessArduinoMessage(message);
                    }
                    
                    // Small delay to prevent CPU hogging
                    await Task.Delay(50, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested, exit gracefully
                    break;
                }
                catch (TimeoutException)
                {
                    // Timeout is expected, just continue
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading from Arduino: {ex.Message}");
                    // Try to reconnect
                    TryReconnect();
                }
            }
        }

        // Proses pesan dari Arduino
        private void ProcessArduinoMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (message.StartsWith("VEHICLE_DETECTED"))
            {
                // Format: VEHICLE_DETECTED:[ENTRY/EXIT]
                VehicleDetected?.Invoke(this, message.Contains("EXIT") ? "EXIT" : "ENTRY");
            }
            else if (message.StartsWith("BUTTON_PRESSED"))
            {
                // Format: BUTTON_PRESSED:[ENTRY/EXIT]
                ButtonPressed?.Invoke(this, message.Contains("EXIT") ? "EXIT" : "ENTRY");
            }
            else if (message.StartsWith("GATE_"))
            {
                // Format: GATE_OPENED atau GATE_CLOSED
                GateStatusChanged?.Invoke(this, message.Contains("OPENED") ? "OPENED" : "CLOSED");
            }
        }

        // Mencoba untuk tersambung kembali jika koneksi terputus
        private void TryReconnect()
        {
            try
            {
                Disconnect();
                Thread.Sleep(1000); // Wait 1 second before reconnecting
                Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reconnect to Arduino: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Disconnect();
            _cancellationTokenSource.Dispose();
        }
    }
} 