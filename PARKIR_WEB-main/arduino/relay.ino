/*
 * ParkIRC Entry Gate Controller
 * 
 * Controls the entry gate for a parking system
 * Interfaces with sensors and communicates with the desktop application
 */

// Define pins
const int RELAY_PIN = 2;       // Relay control pin
const int SENSOR_PIN = 3;      // Vehicle sensor pin (IR sensor or loop detector)
const int BUTTON_PIN = 4;      // Manual button pin
const int LED_PIN = 13;        // Status LED
const int CAMERA_TRIGGER_PIN = 5;  // Pin to trigger external camera (optional)
const int PRINTER_TRIGGER_PIN = 6; // Pin to trigger external thermal printer (optional)

// Timing constants
const unsigned long GATE_OPEN_TIME = 10000;  // Time gate stays open (10 seconds)
const unsigned long DEBOUNCE_DELAY = 50;     // Debounce time for button (50ms)
const unsigned long CAMERA_TRIGGER_TIME = 200; // Camera trigger pulse duration (ms)
const unsigned long PRINTER_TRIGGER_TIME = 500; // Printer trigger pulse duration (ms)

// State variables
bool gateOpen = false;
bool vehicleDetected = false;
bool lastButtonState = HIGH;   // Assuming pull-up resistor
bool buttonState = HIGH;       // Assuming pull-up resistor
unsigned long lastDebounceTime = 0;
unsigned long gateOpenTime = 0;
unsigned long cameraTriggerTime = 0;
unsigned long printerTriggerTime = 0;
bool cameraTriggerActive = false;
bool printerTriggerActive = false;
String ticketData = "";

void setup() {
  // Initialize serial communication
  Serial.begin(9600);
  
  // Initialize pins
  pinMode(RELAY_PIN, OUTPUT);
  pinMode(SENSOR_PIN, INPUT);
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  pinMode(LED_PIN, OUTPUT);
  pinMode(CAMERA_TRIGGER_PIN, OUTPUT);
  pinMode(PRINTER_TRIGGER_PIN, OUTPUT);
  
  // Initialize outputs to inactive state
  digitalWrite(RELAY_PIN, LOW);
  digitalWrite(LED_PIN, LOW);
  digitalWrite(CAMERA_TRIGGER_PIN, LOW);
  digitalWrite(PRINTER_TRIGGER_PIN, LOW);
  
  // Initialize gate to closed position
  closeGate();
  
  Serial.println("Entry Gate Controller initialized");
}

void loop() {
  // Check for commands from desktop application
  checkSerialCommands();
  
  // Check vehicle sensor
  checkVehicleSensor();
  
  // Check manual button
  checkButton();
  
  // Check if it's time to close the gate
  checkGateTimer();
  
  // Check camera and printer trigger timers
  checkCameraTimer();
  checkPrinterTimer();
}

void checkSerialCommands() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    if (command == "OPEN_GATE") {
      openGate();
    } 
    else if (command == "CLOSE_GATE") {
      closeGate();
    }
    else if (command == "STATUS") {
      sendStatus();
    }
    else if (command.startsWith("CAPTURE_IMAGE")) {
      triggerCamera();
    }
    else if (command.startsWith("PRINTV:")) {
      // Extract ticket data from command (format: PRINTV:DATA_TO_PRINT)
      ticketData = command.substring(7);
      triggerPrinter();
    }
  }
}

void checkVehicleSensor() {
  bool currentSensorState = digitalRead(SENSOR_PIN) == LOW; // LOW when vehicle is detected
  
  // If sensor state has changed
  if (currentSensorState != vehicleDetected) {
    vehicleDetected = currentSensorState;
    
    if (vehicleDetected) {
      // Vehicle has been detected
      Serial.println("VEHICLE_DETECTED:ENTRY");
      
      // Auto-trigger camera when vehicle is detected
      triggerCamera();
    } else {
      // Vehicle has left sensor area
      Serial.println("VEHICLE_LEFT:ENTRY");
    }
  }
}

void checkButton() {
  // Read button state with debounce
  int reading = digitalRead(BUTTON_PIN);
  
  if (reading != lastButtonState) {
    lastDebounceTime = millis();
  }
  
  if ((millis() - lastDebounceTime) > DEBOUNCE_DELAY) {
    if (reading != buttonState) {
      buttonState = reading;
      
      // Button is pressed (LOW due to pull-up)
      if (buttonState == LOW) {
        Serial.println("BUTTON_PRESSED:ENTRY");
        toggleGate();
      }
    }
  }
  
  lastButtonState = reading;
}

void checkGateTimer() {
  // Auto-close gate after timeout if it's open
  if (gateOpen && (millis() - gateOpenTime > GATE_OPEN_TIME)) {
    closeGate();
  }
}

void checkCameraTimer() {
  // Turn off camera trigger after specified duration
  if (cameraTriggerActive && (millis() - cameraTriggerTime > CAMERA_TRIGGER_TIME)) {
    digitalWrite(CAMERA_TRIGGER_PIN, LOW);
    cameraTriggerActive = false;
  }
}

void checkPrinterTimer() {
  // Turn off printer trigger after specified duration
  if (printerTriggerActive && (millis() - printerTriggerTime > PRINTER_TRIGGER_TIME)) {
    digitalWrite(PRINTER_TRIGGER_PIN, LOW);
    printerTriggerActive = false;
  }
}

void openGate() {
  if (!gateOpen) {
    digitalWrite(RELAY_PIN, HIGH);  // Activate relay
    digitalWrite(LED_PIN, HIGH);    // Turn on LED
    gateOpen = true;
    gateOpenTime = millis();        // Start timer
    Serial.println("GATE_OPENED");
  }
}

void closeGate() {
  if (gateOpen) {
    digitalWrite(RELAY_PIN, LOW);   // Deactivate relay
    digitalWrite(LED_PIN, LOW);     // Turn off LED
    gateOpen = false;
    Serial.println("GATE_CLOSED");
  }
}

void toggleGate() {
  if (gateOpen) {
    closeGate();
  } else {
    openGate();
  }
}

void triggerCamera() {
  // Activate camera trigger pin (could connect to camera's remote shutter)
  digitalWrite(CAMERA_TRIGGER_PIN, HIGH);
  cameraTriggerTime = millis();
  cameraTriggerActive = true;
  Serial.println("CAMERA_TRIGGERED");
}

void triggerPrinter() {
  // Activate printer trigger pin
  digitalWrite(PRINTER_TRIGGER_PIN, HIGH);
  printerTriggerTime = millis();
  printerTriggerActive = true;
  Serial.println("PRINTING_TICKET:" + ticketData);
}

void sendStatus() {
  Serial.print("STATUS:");
  Serial.print(gateOpen ? "OPEN" : "CLOSED");
  Serial.print(",");
  Serial.println(vehicleDetected ? "VEHICLE_DETECTED" : "NO_VEHICLE");
} 