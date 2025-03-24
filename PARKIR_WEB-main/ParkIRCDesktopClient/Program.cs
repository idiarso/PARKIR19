using System;
using System.Threading.Tasks;
using ParkIRCDesktopClient.Models;
using ParkIRCDesktopClient.Services;

namespace ParkIRCDesktopClient
{
    class Program
    {
        private static ApiService _apiService;
        private static SignalRService _signalRService;
        private static string _baseUrl = "https://localhost:5127/";
        private static string _hubUrl = "https://localhost:5127/parkinghub";

        static async Task Main(string[] args)
        {
            Console.WriteLine("ParkIRC Desktop Client");
            Console.WriteLine("======================");

            _apiService = new ApiService(_baseUrl);
            _signalRService = new SignalRService(_hubUrl);

            // Setup SignalR event handlers
            _signalRService.NotificationReceived += (type, message) =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{type}] {message}");
                Console.ResetColor();
            };

            _signalRService.ParkingUpdateReceived += (update) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Parking update received: {update}");
                Console.ResetColor();
            };

            // Login
            await LoginAsync();

            bool running = true;
            while (running)
            {
                ShowMainMenu();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await ShowDashboardAsync();
                        break;
                    case "2":
                        await RecordVehicleEntryAsync();
                        break;
                    case "3":
                        await RecordVehicleExitAsync();
                        break;
                    case "4":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }

            // Cleanup
            await _signalRService.DisconnectAsync();
        }

        private static void ShowMainMenu()
        {
            Console.WriteLine("\nMain Menu:");
            Console.WriteLine("1. View Dashboard");
            Console.WriteLine("2. Record Vehicle Entry");
            Console.WriteLine("3. Record Vehicle Exit");
            Console.WriteLine("4. Exit");
            Console.Write("Enter your choice: ");
        }

        private static async Task LoginAsync()
        {
            bool authenticated = false;

            while (!authenticated)
            {
                Console.Write("Username: ");
                var username = Console.ReadLine();

                Console.Write("Password: ");
                var password = ReadPassword();

                try
                {
                    var result = await _apiService.LoginAsync(username, password);
                    authenticated = true;
                    Console.WriteLine("Authentication successful!");

                    // Connect to SignalR
                    await _signalRService.ConnectAsync(result.Token);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Authentication failed: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        private static async Task ShowDashboardAsync()
        {
            try
            {
                Console.WriteLine("\nLoading dashboard data...");
                var dashboard = await _apiService.GetDashboardDataAsync();

                Console.WriteLine("\nDashboard:");
                Console.WriteLine($"Total Spaces: {dashboard.TotalSpaces}");
                Console.WriteLine($"Available Spaces: {dashboard.AvailableSpaces}");
                Console.WriteLine($"Occupied Spaces: {dashboard.OccupiedSpaces}");
                Console.WriteLine($"Daily Revenue: ${dashboard.DailyRevenue:N2}");
                Console.WriteLine($"Weekly Revenue: ${dashboard.WeeklyRevenue:N2}");
                Console.WriteLine($"Monthly Revenue: ${dashboard.MonthlyRevenue:N2}");

                Console.WriteLine("\nVehicle Distribution:");
                foreach (var vd in dashboard.VehicleDistribution)
                {
                    Console.WriteLine($"{vd.VehicleType}: {vd.Count}");
                }

                Console.WriteLine("\nRecent Activity:");
                foreach (var activity in dashboard.RecentActivity)
                {
                    Console.WriteLine($"{activity.EntryTime} - {activity.VehicleNumber} ({activity.VehicleType}) - ${activity.TotalAmount:N2}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task RecordVehicleEntryAsync()
        {
            try
            {
                Console.WriteLine("\nRecord Vehicle Entry:");
                
                Console.Write("Vehicle License Plate: ");
                var vehicleNumber = Console.ReadLine();
                
                Console.Write("Vehicle Type (Car, Motorcycle, Truck): ");
                var vehicleType = Console.ReadLine();

                var model = new VehicleEntryModel
                {
                    VehicleNumber = vehicleNumber,
                    VehicleType = vehicleType
                };

                var result = await _apiService.RecordVehicleEntryAsync(model);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nVehicle Entry Recorded Successfully!");
                Console.WriteLine($"Ticket Number: {result.TicketNumber}");
                Console.WriteLine($"Entry Time: {result.EntryTime}");
                Console.WriteLine($"Parking Space: {result.ParkingSpace}");
                Console.WriteLine($"Barcode: {result.BarcodeData}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task RecordVehicleExitAsync()
        {
            try
            {
                Console.WriteLine("\nRecord Vehicle Exit:");
                
                Console.Write("Ticket Number (or leave blank to use license plate): ");
                var ticketNumber = Console.ReadLine();
                
                string vehicleNumber = null;
                if (string.IsNullOrEmpty(ticketNumber))
                {
                    Console.Write("Vehicle License Plate: ");
                    vehicleNumber = Console.ReadLine();
                }
                
                Console.Write("Payment Method (Cash, Card, Mobile): ");
                var paymentMethod = Console.ReadLine();
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    paymentMethod = "Cash";
                }

                var model = new ExitModel
                {
                    TicketNumber = ticketNumber,
                    VehicleNumber = vehicleNumber,
                    PaymentMethod = paymentMethod
                };

                var result = await _apiService.RecordVehicleExitAsync(model);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nVehicle Exit Recorded Successfully!");
                Console.WriteLine($"Vehicle: {result.VehicleNumber}");
                Console.WriteLine($"Entry Time: {result.EntryTime}");
                Console.WriteLine($"Exit Time: {result.ExitTime}");
                Console.WriteLine($"Duration: {result.Duration}");
                Console.WriteLine($"Fee: ${result.ParkingFee:N2}");
                Console.WriteLine($"Payment Method: {result.PaymentMethod}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
} 