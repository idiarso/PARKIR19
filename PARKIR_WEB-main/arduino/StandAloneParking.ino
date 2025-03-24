#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <ArduinoJson.h>
#include <SPI.h>
#include <SD.h>

// Konfigurasi WiFi
const char* ssid = "ParkingSystem";
const char* password = "xxxxx";
const char* serverUrl = "http://your-server.com/api";

// Pin definitions
const int SD_CS_PIN = 5;  // SD Card CS pin
const int BUTTON_PIN = 2; // Push button pin
const int GATE_PIN = 4;   // Gate control pin

// Buffer untuk data offline
struct ParkingData {
  String timestamp;
  String ticketNo;
  bool isUploaded;
};

void setup() {
  Serial.begin(115200);
  
  // Initialize SD Card
  if (!SD.begin(SD_CS_PIN)) {
    Serial.println("SD Card initialization failed!");
    return;
  }

  // Connect to WiFi
  WiFi.begin(ssid, password);
  
  // Setup pins
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  pinMode(GATE_PIN, OUTPUT);
}

void loop() {
  // Check push button
  if (digitalRead(BUTTON_PIN) == LOW) {
    processEntry();
  }
  
  // Try to sync offline data when online
  if (WiFi.status() == WL_CONNECTED) {
    syncOfflineData();
  }
  
  delay(100);
}

void processEntry() {
  String ticketNo = generateTicketNumber();
  String timestamp = getTimestamp();
  
  // Save to SD Card first
  ParkingData data = {timestamp, ticketNo, false};
  saveToSD(data);
  
  // Print ticket
  printTicket(ticketNo, timestamp);
  
  // Open gate
  openGate();
  
  // Try to upload if online
  if (WiFi.status() == WL_CONNECTED) {
    uploadEntry(data);
  }
}

void saveToSD(ParkingData data) {
  File dataFile = SD.open("parking.txt", FILE_WRITE);
  if (dataFile) {
    StaticJsonDocument<200> doc;
    doc["timestamp"] = data.timestamp;
    doc["ticketNo"] = data.ticketNo;
    doc["isUploaded"] = data.isUploaded;
    
    serializeJson(doc, dataFile);
    dataFile.println();
    dataFile.close();
  }
}

void syncOfflineData() {
  File dataFile = SD.open("parking.txt");
  if (dataFile) {
    while (dataFile.available()) {
      String line = dataFile.readStringUntil('\n');
      StaticJsonDocument<200> doc;
      deserializeJson(doc, line);
      
      if (!doc["isUploaded"]) {
        ParkingData data = {
          doc["timestamp"].as<String>(),
          doc["ticketNo"].as<String>(),
          false
        };
        
        if (uploadEntry(data)) {
          // Mark as uploaded in SD
          updateUploadStatus(data.ticketNo);
        }
      }
    }
    dataFile.close();
  }
} 