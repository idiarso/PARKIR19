using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;

namespace ParkIRCDesktopClient.Services
{
    /// <summary>
    /// Service for managing thermal printer integration with the parking system
    /// </summary>
    public class PrinterService
    {
        private string _printerName;
        private bool _isInitialized = false;
        private Font _headerFont;
        private Font _normalFont;
        private Font _footerFont;
        private Font _barcodeFont;
        private string _companyName;
        private string _address;
        private string _phoneNumber;
        private string _footerText;
        
        // Events
        public event EventHandler<PrinterEventArgs> PrintCompleted;
        public event EventHandler<PrinterEventArgs> PrintError;
        
        /// <summary>
        /// Initialize the printer service
        /// </summary>
        /// <param name="printerName">Name of the printer to use (null = default printer)</param>
        public PrinterService(string printerName = null)
        {
            _printerName = printerName;
            InitializeFonts();
            InitializeCompanyInfo();
            _isInitialized = true;
        }
        
        /// <summary>
        /// Initialize the fonts used for printing
        /// </summary>
        private void InitializeFonts()
        {
            _headerFont = new Font("Arial", 12, FontStyle.Bold);
            _normalFont = new Font("Arial", 10, FontStyle.Regular);
            _footerFont = new Font("Arial", 8, FontStyle.Italic);
            
            // If you have a barcode font installed:
            try
            {
                _barcodeFont = new Font("Free 3 of 9 Extended", 24, FontStyle.Regular);
            }
            catch
            {
                // Fallback if no barcode font installed
                _barcodeFont = new Font("Arial", 10, FontStyle.Bold);
            }
        }
        
        /// <summary>
        /// Initialize company information for receipts and tickets
        /// </summary>
        private void InitializeCompanyInfo()
        {
            // This information should be loaded from configuration
            _companyName = "ParkIRC Parking System";
            _address = "123 Parking Street";
            _phoneNumber = "Phone: 555-1234";
            _footerText = "Thank you for using ParkIRC!";
        }
        
        /// <summary>
        /// Get a list of available printers
        /// </summary>
        /// <returns>Array of printer names</returns>
        public string[] GetAvailablePrinters()
        {
            List<string> printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            return printers.ToArray();
        }
        
        /// <summary>
        /// Set the active printer to use
        /// </summary>
        /// <param name="printerName">Printer name</param>
        /// <returns>Success status</returns>
        public bool SetPrinter(string printerName)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                _printerName = null;
                return true;
            }
            
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer == printerName)
                {
                    _printerName = printerName;
                    return true;
                }
            }
            
            OnPrintError($"Printer '{printerName}' not found");
            return false;
        }
        
        /// <summary>
        /// Print an entry ticket with specified data
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="licensePlate">License plate (optional)</param>
        /// <param name="entryTime">Entry timestamp</param>
        /// <returns>Success status</returns>
        public bool PrintEntryTicket(string ticketId, string licensePlate, DateTime entryTime)
        {
            if (!_isInitialized)
            {
                OnPrintError("Printer service not initialized");
                return false;
            }
            
            try
            {
                PrintDocument pd = new PrintDocument();
                
                if (!string.IsNullOrEmpty(_printerName))
                {
                    pd.PrinterSettings.PrinterName = _printerName;
                }
                
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                
                // Store data to use in PrintPage event
                pd.Tag = new TicketData
                {
                    TicketId = ticketId,
                    LicensePlate = licensePlate,
                    Timestamp = entryTime,
                    IsEntryTicket = true
                };
                
                pd.PrintPage += PrintTicketPage;
                pd.Print();
                
                OnPrintCompleted($"Entry ticket {ticketId} printed successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnPrintError($"Error printing entry ticket: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Print an exit receipt with specified data
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="licensePlate">License plate (optional)</param>
        /// <param name="entryTime">Entry timestamp</param>
        /// <param name="exitTime">Exit timestamp</param>
        /// <param name="amountPaid">Amount paid</param>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="operatorId">Operator ID</param>
        /// <returns>Success status</returns>
        public bool PrintExitReceipt(string ticketId, string licensePlate, DateTime entryTime, 
                                     DateTime exitTime, decimal amountPaid, string paymentMethod,
                                     string operatorId)
        {
            if (!_isInitialized)
            {
                OnPrintError("Printer service not initialized");
                return false;
            }
            
            try
            {
                PrintDocument pd = new PrintDocument();
                
                if (!string.IsNullOrEmpty(_printerName))
                {
                    pd.PrinterSettings.PrinterName = _printerName;
                }
                
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                
                // Store data to use in PrintPage event
                pd.Tag = new ReceiptData
                {
                    TicketId = ticketId,
                    LicensePlate = licensePlate,
                    EntryTime = entryTime,
                    ExitTime = exitTime,
                    AmountPaid = amountPaid,
                    PaymentMethod = paymentMethod,
                    OperatorId = operatorId,
                    IsEntryTicket = false
                };
                
                pd.PrintPage += PrintReceiptPage;
                pd.Print();
                
                OnPrintCompleted($"Exit receipt {ticketId} printed successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnPrintError($"Error printing exit receipt: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Handle printing of ticket page
        /// </summary>
        private void PrintTicketPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float yPos = 0;
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float width = e.MarginBounds.Width;
            
            // Get ticket data
            TicketData data = (sender as PrintDocument).Tag as TicketData;
            
            // Company header
            g.DrawString(_companyName, _headerFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _headerFont.GetHeight(g);
            
            g.DrawString(_address, _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString(_phoneNumber, _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g) * 1.5f;
            
            // Ticket info
            g.DrawString("PARKING TICKET", _headerFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _headerFont.GetHeight(g) * 1.5f;
            
            g.DrawString($"Ticket ID: {data.TicketId}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Date: {data.Timestamp.ToShortDateString()}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Time: {data.Timestamp.ToShortTimeString()}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            if (!string.IsNullOrEmpty(data.LicensePlate))
            {
                g.DrawString($"Plate: {data.LicensePlate}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
                yPos += _normalFont.GetHeight(g);
            }
            
            yPos += _normalFont.GetHeight(g);
            
            // Draw separator line
            g.DrawLine(Pens.Black, leftMargin, topMargin + yPos, leftMargin + width, topMargin + yPos);
            yPos += 10;
            
            // Draw barcode (if using barcode font)
            string barcodeValue = $"*{data.TicketId}*";
            g.DrawString(barcodeValue, _barcodeFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _barcodeFont.GetHeight(g) * 1.5f;
            
            // Footer
            g.DrawString("Please keep this ticket with you.", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString("Ticket is required for exit.", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g) * 1.5f;
            
            g.DrawString(_footerText, _footerFont, Brushes.Black, leftMargin, topMargin + yPos);
            
            // No more pages
            e.HasMorePages = false;
        }
        
        /// <summary>
        /// Handle printing of receipt page
        /// </summary>
        private void PrintReceiptPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float yPos = 0;
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            float width = e.MarginBounds.Width;
            
            // Get receipt data
            ReceiptData data = (sender as PrintDocument).Tag as ReceiptData;
            
            // Calculate duration
            TimeSpan duration = data.ExitTime - data.EntryTime;
            string durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";
            
            // Company header
            g.DrawString(_companyName, _headerFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _headerFont.GetHeight(g);
            
            g.DrawString(_address, _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString(_phoneNumber, _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g) * 1.5f;
            
            // Receipt header
            g.DrawString("PARKING RECEIPT", _headerFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _headerFont.GetHeight(g) * 1.5f;
            
            // Receipt details
            g.DrawString($"Ticket ID: {data.TicketId}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            if (!string.IsNullOrEmpty(data.LicensePlate))
            {
                g.DrawString($"License Plate: {data.LicensePlate}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
                yPos += _normalFont.GetHeight(g);
            }
            
            g.DrawString($"Entry: {data.EntryTime}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Exit: {data.ExitTime}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Duration: {durationText}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Amount Paid: {data.AmountPaid:C}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Payment Method: {data.PaymentMethod}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g);
            
            g.DrawString($"Operator: {data.OperatorId}", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g) * 1.5f;
            
            // Draw separator line
            g.DrawLine(Pens.Black, leftMargin, topMargin + yPos, leftMargin + width, topMargin + yPos);
            yPos += 10;
            
            // Thank you message
            g.DrawString("Thank you for using our parking service!", _normalFont, Brushes.Black, leftMargin, topMargin + yPos);
            yPos += _normalFont.GetHeight(g) * 1.5f;
            
            g.DrawString(_footerText, _footerFont, Brushes.Black, leftMargin, topMargin + yPos);
            
            // No more pages
            e.HasMorePages = false;
        }
        
        /// <summary>
        /// Process message from Arduino serial port
        /// </summary>
        /// <param name="messageFromArduino">Message from Arduino</param>
        /// <returns>Should open gate</returns>
        public bool HandleArduinoMessage(string messageFromArduino)
        {
            // Check if this is a vehicle detection message at the exit gate
            if (messageFromArduino == "VEHICLE_DETECTED:EXIT")
            {
                // In a real system, we would:
                // 1. Look up the vehicle in the database
                // 2. Check if payment has been completed
                // 3. Print receipt and open gate if paid
                
                // For demonstration:
                return true;
            }
            
            // Check for printing confirmation
            if (messageFromArduino.StartsWith("PRINTING_TICKET:") ||
                messageFromArduino.StartsWith("PRINTING_RECEIPT:"))
            {
                // The Arduino has triggered its print output
                // In a real system, we could further process this confirmation
                // For now, just log it
                OnPrintCompleted($"Confirmed: {messageFromArduino}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Parse Arduino print command and trigger appropriate print job
        /// </summary>
        /// <param name="printCommand">The PRINTV: command received</param>
        /// <returns>Success status</returns>
        public bool HandlePrintCommand(string printCommand)
        {
            if (!printCommand.StartsWith("PRINTV:"))
            {
                return false;
            }
            
            try
            {
                // Extract data portion
                string data = printCommand.Substring(7);
                
                // Parse data (format: key1:value1,key2:value2,...)
                Dictionary<string, string> values = new Dictionary<string, string>();
                
                string[] pairs = data.Split(',');
                foreach (string pair in pairs)
                {
                    string[] keyValue = pair.Split(':');
                    if (keyValue.Length == 2)
                    {
                        values[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
                
                // Determine if this is an entry ticket or exit receipt
                if (values.ContainsKey("TYPE") && values["TYPE"].ToUpper() == "RECEIPT")
                {
                    // Exit receipt
                    string ticketId = values.ContainsKey("ID") ? values["ID"] : "UNKNOWN";
                    string licensePlate = values.ContainsKey("PLATE") ? values["PLATE"] : "";
                    
                    DateTime entryTime = DateTime.Now.AddHours(-1); // Default to 1 hour ago
                    if (values.ContainsKey("ENTRY"))
                    {
                        DateTime.TryParse(values["ENTRY"], out entryTime);
                    }
                    
                    DateTime exitTime = DateTime.Now;
                    if (values.ContainsKey("EXIT"))
                    {
                        DateTime.TryParse(values["EXIT"], out exitTime);
                    }
                    
                    decimal amount = 0;
                    if (values.ContainsKey("AMOUNT"))
                    {
                        decimal.TryParse(values["AMOUNT"], out amount);
                    }
                    
                    string paymentMethod = values.ContainsKey("PAYMENT") ? values["PAYMENT"] : "CASH";
                    string operatorId = values.ContainsKey("OPERATOR") ? values["OPERATOR"] : "SYSTEM";
                    
                    return PrintExitReceipt(ticketId, licensePlate, entryTime, exitTime, amount, paymentMethod, operatorId);
                }
                else
                {
                    // Entry ticket
                    string ticketId = values.ContainsKey("ID") ? values["ID"] : "T" + DateTime.Now.ToString("yyMMddHHmmss");
                    string licensePlate = values.ContainsKey("PLATE") ? values["PLATE"] : "";
                    
                    DateTime entryTime = DateTime.Now;
                    if (values.ContainsKey("TIME"))
                    {
                        DateTime.TryParse(values["TIME"], out entryTime);
                    }
                    
                    return PrintEntryTicket(ticketId, licensePlate, entryTime);
                }
            }
            catch (Exception ex)
            {
                OnPrintError($"Error processing print command: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Raise the PrintCompleted event
        /// </summary>
        protected virtual void OnPrintCompleted(string message)
        {
            PrintCompleted?.Invoke(this, new PrinterEventArgs
            {
                Success = true,
                Message = message
            });
        }
        
        /// <summary>
        /// Raise the PrintError event
        /// </summary>
        protected virtual void OnPrintError(string errorMessage)
        {
            PrintError?.Invoke(this, new PrinterEventArgs
            {
                Success = false,
                Message = errorMessage
            });
        }
    }
    
    /// <summary>
    /// Event arguments for printer events
    /// </summary>
    public class PrinterEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Data model for entry tickets
    /// </summary>
    public class TicketData
    {
        public string TicketId { get; set; }
        public string LicensePlate { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsEntryTicket { get; set; }
    }
    
    /// <summary>
    /// Data model for exit receipts
    /// </summary>
    public class ReceiptData : TicketData
    {
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }
        public string OperatorId { get; set; }
    }
}

/*
 * INTEGRATION EXAMPLE:
 * 
 * This shows how to integrate the printer with the Arduino serial events:
 *
 * -----------------------------------------------------
 * 
 * // In your EntryGateView.xaml.cs or relevant code file:
 * 
 * private PrinterService _printerService;
 * 
 * public EntryGateView()
 * {
 *     InitializeComponent();
 *     
 *     // Initialize printer service
 *     _printerService = new PrinterService();
 *     
 *     // Get available printers
 *     string[] printers = _printerService.GetAvailablePrinters();
 *     PrinterComboBox.ItemsSource = printers;
 *     
 *     if (printers.Length > 0)
 *     {
 *         PrinterComboBox.SelectedIndex = 0;
 *         _printerService.SetPrinter(printers[0]);
 *     }
 *     
 *     // Subscribe to printer events
 *     _printerService.PrintCompleted += PrinterService_PrintCompleted;
 *     _printerService.PrintError += PrinterService_PrintError;
 *     
 *     // Hook into serial service events
 *     _serialService.EntryGateMessageReceived += SerialService_EntryGateMessageReceived;
 * }
 * 
 * private void SerialService_EntryGateMessageReceived(object sender, string message)
 * {
 *     // Process the message
 *     LogMessage($"Received: {message}");
 *     
 *     // If vehicle detected, print entry ticket
 *     if (message == "VEHICLE_DETECTED:ENTRY")
 *     {
 *         // Generate ticket ID
 *         string ticketId = "T" + DateTime.Now.ToString("yyMMddHHmmss");
 *         
 *         // Format ticket data
 *         string ticketData = $"ID:{ticketId},TIME:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
 *         
 *         // Send print command to Arduino
 *         _serialService.SendCommandToEntryGate("PRINTV:" + ticketData);
 *         
 *         // Print locally
 *         _printerService.PrintEntryTicket(ticketId, null, DateTime.Now);
 *     }
 * }
 * 
 * private void PrinterService_PrintCompleted(object sender, PrinterEventArgs e)
 * {
 *     LogMessage($"Print success: {e.Message}");
 * }
 * 
 * private void PrinterService_PrintError(object sender, PrinterEventArgs e)
 * {
 *     LogMessage($"Print error: {e.Message}");
 * }
 * 
 * private void PrinterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
 * {
 *     if (PrinterComboBox.SelectedItem != null)
 *     {
 *         _printerService.SetPrinter(PrinterComboBox.SelectedItem.ToString());
 *     }
 * }
 * 
 * private void IssueTicketButton_Click(object sender, RoutedEventArgs e)
 * {
 *     string licensePlate = LicensePlateTextBox.Text;
 *     string ticketId = "T" + DateTime.Now.ToString("yyMMddHHmmss");
 *     
 *     // Print local ticket
 *     _printerService.PrintEntryTicket(ticketId, licensePlate, DateTime.Now);
 *     
 *     // Send to Arduino
 *     string ticketData = $"ID:{ticketId},PLATE:{licensePlate},TIME:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
 *     _serialService.SendCommandToEntryGate("PRINTV:" + ticketData);
 * }
 */ 