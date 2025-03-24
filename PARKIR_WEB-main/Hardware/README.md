# Hardware Integration for Parking System

## Overview
This directory contains all the code and utilities needed to integrate the parking system with hardware components such as cameras, gate controllers, and thermal printers.

## Components

### HardwareManager.cs
The main class responsible for hardware integration. It provides a unified interface to interact with all hardware components.

Key features:
- Camera integration (webcam and IP cameras)
- Gate control through serial communication
- Thermal printer support
- Event-based hardware status notifications

### HardwareExample.cs
Contains example implementations for parking entry and exit scenarios:
- ParkingEntryHandler class - demonstrates process flow when a vehicle enters
- ParkingExitHandler class - demonstrates process flow when a vehicle exits

### HardwareTester.cs
A command-line utility to test hardware connections including:
- Camera functionality
- Serial port communication
- Printer connectivity
- Barcode scanner

## Hardware Setup

The hardware integration is designed to work with:

1. **Camera System**
   - Either USB webcam or IP camera
   - Automatically captures vehicle photos at entry
   - Photos are stored with unique ticket IDs for verification
   
2. **Gate Control System**
   - Uses RS232/DB9 communication protocol
   - Connected to ATMEL microcontroller
   - Commands include OPEN_ENTRY, CLOSE_ENTRY, OPEN_EXIT, CLOSE_EXIT
   - Safety features to prevent gate closure when occupied

3. **Thermal Printer**
   - Supports standard 58mm or 80mm thermal printers
   - Prints tickets with barcodes at entry
   - Prints receipts at exit
   
4. **Barcode Scanner**
   - USB HID-compliant barcode scanner
   - Configured to add Enter (CR) after scan
   - Used at exit to retrieve vehicle entry information

## Configuration Files

All hardware components are configured via INI files in the `config` directory:

- `camera.ini` - Camera settings (IP, credentials, resolution)
- `gate.ini` - Gate controller settings (COM port, baud rate, commands)
- `printer.ini` - Printer settings (paper size, ticket format)
- `network.ini` - Database and network settings

## Testing Hardware

To test hardware components, run:
```
test_hardware.bat
```

This will launch a command-line utility that allows you to test each hardware component individually or all components at once.

## Integration Flow

The hardware integration follows this flow:

### Entry Process
1. Vehicle arrives at entry gate
2. Driver or attendant presses the push button
3. ATMEL MCU sends signal to PC via RS232
4. HardwareManager receives signal and triggers entry process
5. Camera captures vehicle image
6. Ticket with barcode is printed
7. Gate opens automatically
8. Vehicle data and image path are stored in PostgreSQL database

### Exit Process
1. Vehicle arrives at exit gate
2. Cashier scans ticket barcode
3. System retrieves entry data including vehicle image
4. Cashier verifies vehicle against entry image
5. System calculates parking fee (if applicable)
6. After payment (if needed), cashier authorizes exit
7. Gate opens automatically
8. Exit receipt is printed
9. Transaction is marked complete in database

## Troubleshooting

Common issues and solutions:

1. **Camera not working**
   - Check camera connection
   - Verify camera is recognized in Device Manager
   - Check camera.ini configuration

2. **Gate not responding**
   - Verify COM port settings
   - Check physical connections
   - Test serial communication with HardwareTester

3. **Printer not printing**
   - Verify printer is online and has paper
   - Check printer driver installation
   - Test printer with HardwareTester

4. **Barcode scanner issues**
   - Ensure scanner is configured to add Enter after scan
   - Check barcode format matches system expectations
   - Test scanner with HardwareTester

## Additional Documentation

For more detailed information, refer to:
- `installation.md` - Step-by-step hardware installation guide
- `system_flow.md` - Detailed system flow diagrams
