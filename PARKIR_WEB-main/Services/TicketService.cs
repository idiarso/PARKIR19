using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.IO;

namespace ParkIRC.Services
{
    public interface ITicketService
    {
        Task<ParkingTicket> GenerateTicketAsync(Vehicle vehicle, string operatorId);
        Task<bool> ValidateTicketAsync(string ticketNumber);
        Task<string> GenerateQRCodeAsync(string data);
    }

    public class TicketService : ITicketService
    {
        private readonly ILogger<TicketService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IPrinterService _printerService;

        public TicketService(
            ILogger<TicketService> logger,
            ApplicationDbContext context,
            IPrinterService printerService)
        {
            _logger = logger;
            _context = context;
            _printerService = printerService;
        }

        public async Task<ParkingTicket> GenerateTicketAsync(Vehicle vehicle, string operatorId)
        {
            try
            {
                var ticketNumber = GenerateTicketNumber();
                var barcodeData = GenerateBarcodeData(vehicle);
                var qrCodeImage = await GenerateQRCodeAsync(barcodeData);

                var ticket = new ParkingTicket
                {
                    TicketNumber = ticketNumber,
                    VehicleId = vehicle.Id,
                    Vehicle = vehicle,
                    EntryTime = DateTime.Now,
                    OperatorId = operatorId,
                    BarcodeData = barcodeData,
                    QRCodeImage = qrCodeImage,
                    IsValid = true
                };

                await _context.ParkingTickets.AddAsync(ticket);
                await _context.SaveChangesAsync();

                await _printerService.PrintTicketAsync(ticket);

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate ticket");
                throw;
            }
        }

        public async Task<bool> ValidateTicketAsync(string ticketNumber)
        {
            try
            {
                var ticket = await _context.ParkingTickets
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

                return ticket != null && ticket.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate ticket");
                return false;
            }
        }

        public async Task<string> GenerateQRCodeAsync(string data)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrCodeData);
                using var qrCodeImage = qrCode.GetGraphic(20);
                using var ms = new MemoryStream();
                qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR code");
                return string.Empty;
            }
        }

        private string GenerateTicketNumber()
        {
            return $"TKT{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private string GenerateBarcodeData(Vehicle vehicle)
        {
            return $"{vehicle.VehicleNumber}|{vehicle.EntryTime:yyyyMMddHHmmss}";
        }
    }
} 