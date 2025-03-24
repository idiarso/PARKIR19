using System;
using System.Windows;

namespace ParkIRCDesktopClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle global exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                MessageBox.Show($"An unhandled exception occurred: {exception?.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Set application-wide properties if needed
            // Application.Current.Properties["ApiBaseUrl"] = "http://192.168.1.100:5127/";
        }
    }
} 