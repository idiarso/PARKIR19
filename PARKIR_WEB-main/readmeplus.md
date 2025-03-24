# Implementasi Sistem Printer ParkIRC

## Prasyarat Instalasi Komputer Lokal

### A. Komputer Server Utama

1. **Sistem Operasi**
   - Ubuntu Server 20.04 LTS atau lebih tinggi
   - Minimal RAM 8GB
   - Storage 500GB SSD
   - Processor 4 Core

2. **Software Requirements**
   ```bash
   # Update sistem
   sudo apt update && sudo apt upgrade -y

   # Install PostgreSQL
   sudo apt install postgresql postgresql-contrib -y

   # Install .NET 6.0
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt update
   sudo apt install dotnet-sdk-6.0 -y

   # Install Nginx
   sudo apt install nginx -y

   # Install SSL
   sudo apt install certbot python3-certbot-nginx -y
   ```

3. **Network Requirements**
   - IP Statis
   - Port yang harus dibuka:
     - 80/443 (HTTP/HTTPS)
     - 5432 (PostgreSQL)
     - 5000 (API)
     - 8080 (WebSocket)

### B. Komputer Pintu Masuk (Entry Gate)

1. **Sistem Operasi**
   - Windows 10 Pro 64-bit
   - Minimal RAM 4GB
   - Storage 256GB SSD
   - Processor 2 Core

2. **Software Requirements**
   - .NET 6.0 Desktop Runtime
   - PostgreSQL 13 atau lebih tinggi
   - Printer Driver (Epson/Custom)
   - Chrome/Firefox terbaru

3. **Hardware Requirements**
   - Thermal Printer
   - Push Button
   - Gate Barrier
   - IP Camera (opsional)
   - UPS

4. **Instalasi Software**
   ```powershell
   # Install Chocolatey
   Set-ExecutionPolicy Bypass -Scope Process -Force
   [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
   iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

   # Install requirements menggunakan Chocolatey
   choco install dotnet-6.0-desktopruntime -y
   choco install postgresql13 -y
   choco install googlechrome -y
   choco install notepadplusplus -y
   ```

5. **Konfigurasi Printer**
   ```bash
   # Install printer driver
   ./setup-printer.sh

   # Test printer
   dotnet run --project PrinterTest
   ```

### C. Konfigurasi Database

1. **Server Utama**
   ```sql
   -- Buat user dan database
   CREATE USER parkirc WITH PASSWORD 'your_password';
   CREATE DATABASE parkirc_main;
   GRANT ALL PRIVILEGES ON DATABASE parkirc_main TO parkirc;

   -- Enable remote access
   -- Edit pg_hba.conf untuk mengizinkan koneksi dari IP komputer entry
   ```

2. **Komputer Entry Gate**
   ```sql
   -- Buat database lokal
   CREATE DATABASE parkirc_local;

   -- Buat tabel untuk data lokal
   CREATE TABLE local_transactions (
       id SERIAL PRIMARY KEY,
       ticket_number VARCHAR(50) NOT NULL,
       entry_time TIMESTAMP NOT NULL,
       vehicle_number VARCHAR(20),
       is_synced BOOLEAN DEFAULT FALSE,
       sync_time TIMESTAMP,
       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
   );
   ```

### D. Konfigurasi Jaringan

1. **Server Utama**
   ```bash
   # Setup Firewall
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw allow 5432/tcp
   sudo ufw allow 5000/tcp
   sudo ufw allow 8080/tcp
   sudo ufw enable
   ```

2. **Komputer Entry Gate**
   - Set IP Statis
   - Konfigurasi DNS ke server utama
   - Test koneksi ke server utama

### E. Monitoring & Backup

1. **Setup Monitoring**
   ```bash
   # Install monitoring tools
   sudo apt install prometheus node-exporter -y
   sudo apt install grafana -y
   ```

2. **Setup Backup**
   ```bash
   # Buat script backup
   cat > backup.sh << 'EOF'
   #!/bin/bash
   DATE=$(date +%Y%m%d)
   # Backup database
   pg_dump parkirc_local > /backup/db_$DATE.sql
   # Backup config
   cp appsettings.json /backup/config_$DATE.json
   # Compress
   tar -czf /backup/backup_$DATE.tar.gz /backup/db_$DATE.sql /backup/config_$DATE.json
   # Cleanup old backups
   find /backup -type f -name "backup_*.tar.gz" -mtime +7 -delete
   EOF

   chmod +x backup.sh
   ```

### F. Security

1. **SSL Certificates**
   ```bash
   # Generate SSL untuk server utama
   sudo certbot --nginx -d your-domain.com
   ```

2. **Database Security**
   ```sql
   -- Enkripsi password
   ALTER SYSTEM SET ssl = on;
   -- Restart PostgreSQL untuk menerapkan perubahan
   ```

3. **Application Security**
   - Setup JWT authentication
   - Implement rate limiting
   - Enable audit logging

### G. Testing

1. **Test Koneksi**
   ```bash
   # Test database
   psql -h localhost -U parkirc -d parkirc_local

   # Test API
   curl https://main-server/api/health

   # Test printer
   lp -d EPSON_TM_T82 test_page.txt
   ```

2. **Test Sinkronisasi**
   - Verifikasi data tersimpan di local DB
   - Cek sinkronisasi ke server utama
   - Validasi backup berjalan

### H. Troubleshooting

1. **Log Files**
   - Application: `/var/log/parkirc/app.log`
   - Database: `/var/log/postgresql/postgresql.log`
   - Nginx: `/var/log/nginx/error.log`

2. **Common Issues**
   - Database connection issues
   - Printer not responding
   - Sync failures
   - Network connectivity

3. **Monitoring Alerts**
   - Setup email notifications
   - Configure SMS alerts
   - Dashboard monitoring

## Arsitektur Printer

Sistem printer ParkIRC mendukung multiple sumber koneksi:

1. **WebSocket**
   - Koneksi real-time dengan printer
   - Mendukung status monitoring
   - Auto-reconnect saat koneksi terputus

2. **Serial/USB**
   - Koneksi langsung via port COM
   - Deteksi printer otomatis
   - Status monitoring via polling

3. **Network Printer**
   - Koneksi via TCP/IP
   - Support untuk printer jaringan
   - Port 9100 untuk raw printing

## Konfigurasi Printer

### 1. Database (PostgreSQL)
```sql
CREATE TABLE PrinterConfigs (
    Id varchar(50) PRIMARY KEY,
    Name varchar(100) NOT NULL,
    Port varchar(50),
    IsActive boolean DEFAULT true,
    IpAddress varchar(50),
    TcpPort int,
    ConnectionType varchar(20),
    Status varchar(20),
    LastChecked timestamp
);
```

### 2. JSON Config (printer_config.json)
```json
{
  "printers": [
    {
      "id": "ENTRY1",
      "name": "Entry Gate Printer 1",
      "port": "COM3",
      "isActive": true,
      "connectionType": "Serial"
    },
    {
      "id": "EXIT1",
      "name": "Exit Gate Printer 1",
      "ipAddress": "192.168.1.100",
      "tcpPort": 9100,
      "isActive": true,
      "connectionType": "Network"
    }
  ],
  "websocket": {
    "url": "ws://localhost:8080",
    "reconnectInterval": 5000
  }
}
```

### 3. Environment Variables
```bash
PRINTER_WEBSOCKET_URL=ws://localhost:8080
PRINTER_RECONNECT_INTERVAL=5000
PRINTER_DEFAULT_PORT=COM3
```

## Alur Kerja Printer

1. **Inisialisasi**
   ```mermaid
   graph TD
   A[Start] --> B{Try WebSocket}
   B -->|Success| C[Use WebSocket]
   B -->|Fail| D{Try JSON Config}
   D -->|Success| E[Use JSON Config]
   D -->|Fail| F[Use Database Config]
   ```

2. **Monitoring Status**
   ```mermaid
   graph TD
   A[Start Monitor] --> B{WebSocket Active?}
   B -->|Yes| C[Monitor via WebSocket]
   B -->|No| D[Monitor via Serial/Poll]
   C --> E{Status Changed?}
   D --> E
   E -->|Yes| F[Update Status]
   F --> G[Notify UI]
   ```

## Penggunaan dalam Kode

### 1. Print Tiket
```csharp
// Di controller
public async Task<IActionResult> PrintEntryTicket([FromBody] TicketData data)
{
    var success = await _printerService.PrintTicket("ENTRY1", data);
    return success ? Ok() : BadRequest();
}
```

### 2. Cek Status
```csharp
// Di service
public Dictionary<string, PrinterStatus> GetAllPrinterStatus()
{
    return _printerStatuses;
}
```

### 3. Monitoring UI
```javascript
// Di client
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/printerHub")
    .build();

connection.on("PrinterStatusChanged", (status) => {
    updatePrinterStatus(status);
});
```

## Troubleshooting

### 1. WebSocket Issues
- Check koneksi ke printer WebSocket server
- Verifikasi port dan firewall
- Cek log di `printer_websocket.log`

### 2. Serial Port Issues
- Verifikasi port COM yang benar
- Check driver printer
- Test koneksi manual dengan `mode COM3`

### 3. Network Printer Issues
- Ping printer IP
- Verifikasi port 9100 terbuka
- Check printer dalam jaringan yang sama

## Maintenance

### 1. Log Files
- Application logs: `/var/log/parkirc/printer.log`
- WebSocket logs: `/var/log/parkirc/printer_websocket.log`
- Error logs: `/var/log/parkirc/printer_error.log`

### 2. Backup Config
```bash
# Backup printer config
cp printer_config.json printer_config.backup.json

# Backup database config
pg_dump -t printer_configs > printer_configs.sql
```

### 3. Monitoring
- Check status: `http://localhost:5000/management/printer-status`
- WebSocket status: `ws://localhost:8080/status`
- Hardware status: COM port atau network connectivity

## Security

1. **Authentication**
   - WebSocket connections require token
   - API endpoints protected with JWT
   - Printer commands validated

2. **Network**
   - Printer network isolated
   - Firewall rules for printer ports
   - SSL/TLS for network printers

3. **Logging**
   - All print jobs logged
   - Failed attempts recorded
   - Audit trail maintained

## Development

### Local Setup
1. Install dependencies:
```bash
dotnet add package System.IO.Ports
dotnet add package Newtonsoft.Json
```

2. Configure printer:
```json
{
  "PrinterSettings": {
    "WebSocketUrl": "ws://localhost:8080",
    "DefaultPrinter": "ENTRY1"
  }
}
```

3. Run tests:
```bash
dotnet test PrinterTests.cs
```

### Production Deployment
1. Update configs
2. Set environment variables
3. Start printer service
4. Monitor logs

## Referensi

- [Dokumentasi Printer Service](docs/printer-service.md)
- [WebSocket Protocol](docs/websocket-protocol.md)
- [Hardware Setup](docs/hardware-setup.md)
- [Troubleshooting Guide](docs/troubleshooting.md)

## Implementasi Kamera Entry Gate (Non-ANPR)

### A. Arsitektur Sistem

1. **Komponen Utama**
   - Kamera USB/IP Camera (ditangani Web Application)
   - Interface Operator
   - Storage Gambar
   - Validasi Entry

2. **Penanganan Kamera**
   - Dikelola oleh aplikasi web menggunakan Web API
   - Menggunakan mediaDevices.getUserMedia untuk akses kamera
   - Preview real-time di interface operator
   - Capture dan processing di sisi aplikasi
   - Tidak memerlukan Arduino untuk kamera

3. **Keuntungan Penanganan via Aplikasi**
   - Interface yang lebih user-friendly
   - Pemrosesan gambar lebih fleksibel
   - Integrasi database langsung
   - Manajemen storage terpusat
   - Backup dan monitoring terintegrasi

### B. Implementasi Interface

1. **Operator View**
   ```html
   <!-- Views/EntryGate/Operator.cshtml -->
   <div class="container">
       <!-- Live Camera Feed -->
       <div id="camera-feed"></div>
       
       <!-- Quick Entry Form -->
       <form id="quickEntryForm">
           <!-- Preset Vehicle Type -->
           <button type="button" onclick="setVehicleType('Motor')">
               <i class="fas fa-motorcycle"></i> Motor
           </button>
           
           <!-- Plate Number Input -->
           <input type="text" id="plateNumber" 
                  placeholder="B 1234 ABC"
                  onkeyup="autoFormatPlate(this)">
       </form>
   </div>
   ```

2. **JavaScript Helper**
   ```javascript
   // Format plat nomor otomatis
   autoFormatPlate(input) {
       let value = input.value.toUpperCase();
       // Format: B 1234 ABC
       if (value.length > 0) {
           // Area code + Angka + Huruf akhir
           let formatted = formatPlateNumber(value);
           input.value = formatted;
       }
   }
   
   // Capture foto
   async captureImage() {
       const stream = await navigator.mediaDevices.getUserMedia({ video: true });
       const video = document.getElementById('camera-feed');
       // Capture dan convert ke base64
       const imageData = await captureVideoFrame(video);
       return imageData;
   }
   ```

### C. Backend Processing

1. **Controller**
   ```csharp
   [HttpPost]
   public async Task<IActionResult> CaptureEntry([FromBody] EntryRequest request)
   {
       // 1. Capture dan simpan foto
       var imageBytes = await _cameraService.CaptureImage();
       string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{request.GateId}.jpg";
       await _storageService.SaveImage(imageBytes, fileName);
       
       // 2. Buat transaksi parkir
       var transaction = new ParkingTransaction {
           TicketNumber = GenerateTicketNumber(),
           VehicleImagePath = fileName,
           VehicleNumber = request.VehicleNumber,
           IsManualEntry = true
       };
       
       // 3. Simpan ke database
       await _context.ParkingTransactions.AddAsync(transaction);
   }
   ```

2. **Validasi Entry**
   ```csharp
   public class EntryValidationService
   {
       public async Task<ValidationResult> ValidateEntry(string plateNumber)
       {
           // 1. Validasi format plat
           if (!IsValidPlateFormat(plateNumber))
               return InvalidResult("Format plat tidak valid");
               
           // 2. Cek duplikasi (masih di dalam)
           if (await IsVehicleInside(plateNumber))
               return InvalidResult("Kendaraan masih di dalam");
               
           // 3. Log entry
           await LogEntry(plateNumber);
           
           return ValidResult();
       }
   }
   ```

### D. Storage & Backup

1. **Struktur Folder**
   ```
   /images
     /entries
       /YYYYMMDD/
         - HHmmss_GATE1.jpg
         - HHmmss_GATE2.jpg
   ```

2. **Backup Strategy**
   - Daily backup gambar ke external storage
   - Compress gambar > 30 hari
   - Delete gambar > 90 hari

### E. Troubleshooting

1. **Kamera Issues**
   - Check device permissions
   - Verify camera connection
   - Test dengan software lain

2. **Storage Issues**
   - Monitor disk space
   - Check file permissions
   - Verify backup process

3. **Performance**
   - Optimize image size
   - Index database queries
   - Clean old records

### F. Security

1. **Image Storage**
   - Restrict folder access
   - Encrypt sensitive data
   - Regular security audit

2. **Access Control**
   - Role-based access
   - Activity logging
   - Session management

### G. Maintenance

1. **Daily Tasks**
   - Check camera status
   - Verify storage space
   - Monitor error logs

2. **Weekly Tasks**
   - Clean temporary files
   - Backup configuration
   - Update documentation

3. **Monthly Tasks**
   - Review performance
   - Optimize database
   - Update security patches

## Teknologi

...

## Pembagian Tugas Hardware

### A. Tugas Arduino
1. **Push Button**
   ```cpp
   // Menangani input push button
   void handlePushButton() {
       if (digitalRead(BUTTON_PIN) == LOW) {
           // Kirim sinyal ke aplikasi
           sendEntryRequest();
           // Tunggu sampai button dilepas
           while(digitalRead(BUTTON_PIN) == LOW) delay(10);
       }
   }
   ```

2. **Gate Barrier Control**
   ```cpp
   // Kontrol gate barrier
   void controlGate(bool open) {
       if (open) {
           digitalWrite(GATE_PIN, HIGH);
           delay(GATE_OPEN_TIME);
           digitalWrite(GATE_PIN, LOW);
       }
   }
   ```

3. **Komunikasi dengan Printer**
   ```cpp
   // Handling printer commands
   void handlePrinter(String command) {
       if (command.startsWith("PRINT")) {
           // Parse ticket data
           String ticketData = command.substring(6);
           // Send to printer via serial
           printerSerial.println(ticketData);
       }
   }
   ```

4. **Sensor Kendaraan**
   ```cpp
   // Loop sensor kendaraan
   void checkVehicleSensor() {
       if (digitalRead(SENSOR_PIN) == HIGH) {
           // Vehicle detected
           vehiclePresent = true;
           notifyApplication();
       }
   }
   ```

### B. Komunikasi Arduino-Aplikasi

1. **Serial Protocol**
   ```cpp
   // Format pesan: CMD|DATA
   // Contoh: ENTRY|GATE1 atau PRINT|TICKET123
   void processSerialCommand() {
       if (Serial.available()) {
           String cmd = Serial.readStringUntil('|');
           String data = Serial.readStringUntil('\n');
           executeCommand(cmd, data);
       }
   }
   ```

2. **Status Reporting**
   ```cpp
   // Kirim status ke aplikasi
   void reportStatus() {
       String status = String("STATUS|") +
                      String(digitalRead(GATE_PIN)) + "|" +
                      String(digitalRead(SENSOR_PIN)) + "|" +
                      String(printerStatus);
       Serial.println(status);
   }
   ```

### C. Wiring Diagram
```
Arduino Mega
├── Digital Pin 2  -> Push Button
├── Digital Pin 3  -> Gate Control Relay
├── Digital Pin 4  -> Vehicle Sensor
├── Serial1 TX/RX -> Printer
└── USB           -> Computer
```

### D. Error Handling
1. **Hardware Errors**
   - Printer jam/paper out
   - Gate malfunction
   - Sensor errors

2. **Communication Errors**
   - Serial timeout
   - Command validation
   - Retry mechanism

### E. Maintenance
1. **Daily Checks**
   - Test push button
   - Verify gate operation
   - Check printer paper

2. **Periodic Maintenance**
   - Clean sensors
   - Check connections
   - Update firmware if needed

### B. Implementasi Interface
... 