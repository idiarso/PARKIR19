# Arduino Parking Gate Control System

## Overview

This directory contains the Arduino code for controlling the parking gate system. The system consists of:

1. **Entry Gate Controller** (`relay.ino`): Controls the entry gate with relay, sensor, and button
2. **Exit Gate Controller** (`relay_exit.ino`): Controls the exit gate with similar functionality
3. **Stand-Alone Parking Controller** (`StandAloneParking.ino`): A simplified version for testing

## Hardware Requirements

- Arduino Uno or compatible board (2 units - one for entry, one for exit)
- Relay module (5V compatible with Arduino)
- IR sensor or loop detector for vehicle detection
- Push button for manual control
- Status LED
- Gate motor/actuator (connected to relay)
- Power supply for Arduino and gate motor
- USB cables for connection to desktop application

## Wiring Instructions

### Entry/Exit Gate Controllers

Connect the components as follows:

| Component | Arduino Pin |
|-----------|-------------|
| Relay control | Digital Pin 2 |
| Vehicle sensor | Digital Pin 3 |
| Manual button | Digital Pin 4 |
| Status LED | Digital Pin 13 |

## Installation and Setup

1. **Install Arduino IDE**
   - Download and install from [arduino.cc](https://www.arduino.cc/en/software)

2. **Connect Arduino boards**
   - Connect entry gate Arduino to computer via USB
   - Connect exit gate Arduino to computer via USB (or second USB port)

3. **Upload code**
   - Open `relay.ino` in Arduino IDE
   - Select correct board and port
   - Click Upload
   - Repeat with `relay_exit.ino` for the exit gate controller

4. **Test functionality**
   - The status LED should blink briefly at startup
   - Press the manual button to test relay activation
   - Trigger the sensor to test detection

## Integration with Desktop Application

The Arduino controllers communicate with the desktop application using serial communication at 9600 baud rate. The communication protocol is detailed in the `Arduino_Gate_API_Documentation.md` file.

### Arduino Commands

The Arduino controllers respond to the following commands from the desktop application:
- `OPEN_GATE` - Opens the gate
- `CLOSE_GATE` - Closes the gate
- `STATUS` - Returns the current status of the gate and sensor

### Arduino Events

The Arduino controllers send these events to the desktop application:
- `GATE_OPENED` - Gate has been opened
- `GATE_CLOSED` - Gate has been closed
- `VEHICLE_DETECTED:ENTRY` or `VEHICLE_DETECTED:EXIT` - Vehicle detected at sensor
- `VEHICLE_LEFT:ENTRY` or `VEHICLE_LEFT:EXIT` - Vehicle left sensor area
- `BUTTON_PRESSED:ENTRY` or `BUTTON_PRESSED:EXIT` - Manual button pressed

## Customization Options

### Timing Adjustments

You can modify these constants in the Arduino code:
- `GATE_OPEN_TIME` - Duration gate stays open (default 10 seconds)
- `DEBOUNCE_DELAY` - Button debounce time (default 50ms)

### Sensor Logic

If your sensor has different logic (e.g., HIGH when activated instead of LOW):
- Modify `checkVehicleSensor()` function to invert the logic

## Troubleshooting

### Common Issues and Solutions

1. **Gate not responding**
   - Check relay wiring
   - Verify relay pin configuration
   - Test with manual button

2. **Sensor not detecting**
   - Check wiring and power to sensor
   - Adjust sensor sensitivity/position
   - Verify logic (HIGH/LOW) in code matches your sensor

3. **Communication issues**
   - Verify correct COM port selection
   - Ensure baud rate is set to 9600
   - Check USB connections

4. **Erratic behavior**
   - Check power supply stability
   - Verify no interference from motors/relays
   - Add capacitors to filter noise if needed

## Advanced Integration

For integration with camera systems, payment systems, or web interfaces, refer to the `Arduino_Gate_API_Documentation.md` file that provides JSON API examples.

## License and Attribution

This code is provided for educational and implementation purposes as part of the ParkIRC parking management system.

## Support

For additional help, please refer to:
- `Arduino_Gate_API_Documentation.md` - Detailed API documentation
- `CameraIntegration.cs` - Example code for camera integration 