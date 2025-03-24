using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace ParkIRCDesktopClient.Services
{
    public class SignalRService
    {
        private HubConnection _hubConnection;
        private readonly string _hubUrl;
        private string _token;

        public event Action<string, string> NotificationReceived;
        public event Action<object> ParkingUpdateReceived;
        public event Action<object> SystemStatusReceived;

        public SignalRService(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string token)
        {
            if (_hubConnection != null)
            {
                await DisconnectAsync();
            }

            _token = token;
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_hubUrl}?access_token={_token}")
                .WithAutomaticReconnect()
                .Build();

            // Register event handlers
            _hubConnection.On<string, string>("ReceiveNotification", (type, message) =>
            {
                NotificationReceived?.Invoke(type, message);
            });

            _hubConnection.On<object>("ReceiveParkingUpdate", (update) =>
            {
                ParkingUpdateReceived?.Invoke(update);
            });

            _hubConnection.On<object>("ReceiveSystemStatus", (status) =>
            {
                SystemStatusReceived?.Invoke(status);
            });

            // Start connection
            await _hubConnection.StartAsync();
            Console.WriteLine("Connected to SignalR hub");
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                Console.WriteLine("Disconnected from SignalR hub");
            }
        }

        public async Task RequestSystemStatusAsync()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to SignalR hub");
            }

            await _hubConnection.InvokeAsync("RequestSystemStatus");
        }
    }
} 