# Arduino Gate API Documentation

## Overview

This document describes the API for integrating with the ParkIRC entry and exit gate Arduino controllers. The Arduino controllers manage the physical barriers and sensors at parking entrances and exits, while communicating with the main system via serial communication.

## Communication Protocol

- **Connection Type**: Serial over USB
- **Baud Rate**: 9600
- **Message Format**: Text-based commands and responses terminated with newline character (`\n`)

## Hardware Integration

### Pin Configuration

#### Entry/Exit Gate Controllers
| Pin | Function |
|-----|----------|
| 2 | Relay control (gate operation) |
| 3 | Vehicle sensor (IR or loop detector) |
| 4 | Manual button |
| 5 | Camera trigger output |
| 6 | Printer trigger output |
| 13 | Status LED |

## Commands (Desktop App → Arduino)

The Arduino controllers accept the following commands from the desktop application:

| Command | Description | Response |
|---------|-------------|----------|
| `OPEN_GATE` | Opens the gate | `GATE_OPENED` |
| `CLOSE_GATE` | Closes the gate | `GATE_CLOSED` |
| `STATUS` | Requests the current status of the gate and sensor | `STATUS:OPEN/CLOSED,VEHICLE_DETECTED/NO_VEHICLE` |
| `CAPTURE_IMAGE` | Triggers the camera to capture an image | `CAMERA_TRIGGERED` |
| `PRINTV:DATA` | Prints a ticket/receipt with provided data | `PRINTING_TICKET:DATA` or `PRINTING_RECEIPT:DATA` |

For the `PRINTV:` command, replace `DATA` with the actual text to print. The data will be echoed back in the response.

Example:
```
PRINTV:ID:12345,TIME:14:30,DATE:2023-03-24
```

Response:
```
PRINTING_TICKET:ID:12345,TIME:14:30,DATE:2023-03-24
```

## Events (Arduino → Desktop App)

The Arduino controllers automatically send the following events to notify the desktop application of state changes:

| Event | Description | Format |
|-------|-------------|--------|
| `GATE_OPENED` | Notification that gate has been opened | `GATE_OPENED` |
| `GATE_CLOSED` | Notification that gate has been closed | `GATE_CLOSED` |
| `VEHICLE_DETECTED:ENTRY` | Vehicle detected at entry gate sensor | `VEHICLE_DETECTED:ENTRY` |
| `VEHICLE_LEFT:ENTRY` | Vehicle left entry gate sensor area | `VEHICLE_LEFT:ENTRY` |
| `VEHICLE_DETECTED:EXIT` | Vehicle detected at exit gate sensor | `VEHICLE_DETECTED:EXIT` |
| `VEHICLE_LEFT:EXIT` | Vehicle left exit gate sensor area | `VEHICLE_LEFT:EXIT` |
| `BUTTON_PRESSED:ENTRY` | Manual button pressed at entry gate | `BUTTON_PRESSED:ENTRY` |
| `BUTTON_PRESSED:EXIT` | Manual button pressed at exit gate | `BUTTON_PRESSED:EXIT` |
| `CAMERA_TRIGGERED` | Camera trigger has been activated | `CAMERA_TRIGGERED` |
| `PRINTING_TICKET:DATA` | Entry ticket is being printed with provided data | `PRINTING_TICKET:DATA` |
| `PRINTING_RECEIPT:DATA` | Exit receipt is being printed with provided data | `PRINTING_RECEIPT:DATA` |

## JSON API (REST Integration)

For integrating with web services or other systems that may not have direct serial access, you can implement a REST API bridge that translates between serial commands and HTTP requests. Below is the recommended JSON format:

### Commands (HTTP → Gate Controller)

**Endpoint**: `POST /api/gates/{gate_id}/command`

Request body:
```json
{
  "command": "OPEN_GATE|CLOSE_GATE|STATUS|CAPTURE_IMAGE|PRINTV",
  "data": "ID:12345,TIME:14:30,DATE:2023-03-24",  // Optional, only for PRINTV command
  "timestamp": "2023-03-24T15:45:30Z"
}
```

Response:
```json
{
  "success": true,
  "message": "Command sent successfully",
  "command": "OPEN_GATE",
  "gate_id": "entry",
  "timestamp": "2023-03-24T15:45:31Z"
}
```

### Camera Capture (HTTP → Gate Controller)

**Endpoint**: `POST /api/gates/{gate_id}/camera/capture`

Request body:
```json
{
  "reason": "ENTRY|EXIT|MANUAL",
  "metadata": {
    "ticket_id": "T12345",
    "license_plate": "ABC123"  // Optional
  },
  "timestamp": "2023-03-24T15:45:30Z"
}
```

Response:
```json
{
  "success": true,
  "message": "Image capture triggered",
  "gate_id": "entry",
  "timestamp": "2023-03-24T15:45:31Z",
  "image_info": {
    "expected_path": "/images/vehicles/entry_T12345_20230324154531.jpg"
  }
}
```

### Print Ticket/Receipt (HTTP → Gate Controller)

**Endpoint**: `POST /api/gates/{gate_id}/print`

Request body:
```json
{
  "type": "TICKET|RECEIPT",
  "data": {
    "id": "T12345",
    "timestamp": "2023-03-24T15:45:30Z",
    "license_plate": "ABC123",
    "amount_paid": 15000,
    "duration": "1h 30m",
    "operator": "OP001"
  }
}
```

Response:
```json
{
  "success": true,
  "message": "Print job sent",
  "gate_id": "entry",
  "print_type": "TICKET",
  "timestamp": "2023-03-24T15:45:31Z"
}
```

### Status (HTTP GET)

**Endpoint**: `GET /api/gates/{gate_id}/status`

Response:
```json
{
  "gate_id": "entry|exit",
  "status": {
    "gate": "OPEN|CLOSED",
    "sensor": "VEHICLE_DETECTED|NO_VEHICLE",
    "last_camera_trigger": "2023-03-24T15:44:20Z",
    "last_print_job": "2023-03-24T15:44:25Z"
  },
  "last_updated": "2023-03-24T15:45:30Z"
}
```

## Implementation Notes

1. **Camera Control**: The Arduino provides a trigger signal on pin 5 that can be connected to a camera's remote shutter input or used to signal the desktop application to take a picture
2. **Printer Control**: The Arduino provides a trigger signal on pin 6 that can control a thermal printer or signal the desktop application to print a ticket/receipt
3. **Timing**: Camera and printer signals are active for 200ms and 500ms respectively
4. **Auto-triggers**: 
   - When a vehicle is detected at either gate, the camera will automatically trigger
   - The printer must be triggered explicitly with the `PRINTV:` command
5. **Desktop Integration**: The desktop application should listen for the Arduino's responses and events and respond accordingly

## Example Integration Code

### Serial Command Examples
```csharp
// C# examples
// Open gate
serialPort.WriteLine("OPEN_GATE");

// Request status
serialPort.WriteLine("STATUS");

// Trigger camera
serialPort.WriteLine("CAPTURE_IMAGE");

// Print ticket
serialPort.WriteLine("PRINTV:ID:T12345,TIME:14:30,DATE:2023-03-24,PLATE:ABC123");
```

### Camera Integration

While hardware cameras can be triggered directly by the Arduino, most implementations will use a software camera connected to the desktop computer. In this case, you should:

1. Listen for the `VEHICLE_DETECTED:ENTRY` event from the Arduino
2. Trigger the camera capture from your application
3. Process and store the captured image

Example code for camera integration can be found in the `CameraIntegration.cs` file.

### Printer Integration

Thermal printers can be connected to:

1. The Arduino directly (requires additional hardware and library integration)
2. The desktop computer (recommended for complex tickets)

When using a desktop-connected printer, your application should:
1. Format the ticket data
2. Send `PRINTV:` command with the data to the Arduino
3. Watch for the `PRINTING_TICKET:` or `PRINTING_RECEIPT:` response
4. Print using the system printer

## Troubleshooting

| Problem | Possible Solution |
|---------|-------------------|
| No response from Arduino | Check USB connection and COM port configuration |
| Gate doesn't move | Verify relay connections and power supply |
| Sensor not detecting | Check sensor wiring and sensitivity settings |
| Camera not triggering | Check pin 5 connections and camera interface |
| Printer not working | Check pin 6 connections and printer power/drivers | 