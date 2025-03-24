# Pengaturan Printer dan Hardware

## Daftar Isi
1. [Persyaratan Hardware](#persyaratan-hardware)
2. [Konfigurasi Printer](#konfigurasi-printer)
3. [Konfigurasi Kamera](#konfigurasi-kamera)
4. [Loop Detector dan Gate Controller](#loop-detector-dan-gate-controller)
5. [Mikrokontroler](#mikrokontroler)
6. [Deteksi Sinyal dan Troubleshooting](#deteksi-sinyal-dan-troubleshooting)

## Persyaratan Hardware

### Komputer Client (ParkingIN dan ParkingOut)
- Prosesor: Intel Core i3 atau setara (minimal)
- RAM: 4GB (minimal)
- Storage: 128GB SSD (direkomendasikan)
- OS: Windows 10 64-bit
- Port: Minimal 2 port USB (untuk kamera dan printer), 2 port Serial/USB-to-Serial (untuk loop detector dan gate controller)
- Jaringan: Ethernet 100Mbps atau Wi-Fi 802.11n

### Server Database
- Prosesor: Intel Core i5 atau setara (minimal)
- RAM: 8GB (minimal)
- Storage: 256GB SSD dengan backup eksternal
- OS: Windows Server 2016/2019 atau Windows 10 Pro
- Network: Ethernet 1Gbps

### Perangkat Tambahan
- Webcam HD (resolusi minimal 720p)
- Printer Thermal (58mm atau 80mm)
- Loop detector (untuk deteksi kendaraan)
- Gate controller (untuk kendali palang pintu)
- Mikrokontroler Arduino/ESP32 (opsional, untuk pengembangan lebih lanjut)
- UPS (Uninterruptible Power Supply)

## Konfigurasi Printer

### Printer yang Didukung
Sistem Parkir Modern mendukung berbagai printer thermal dengan spesifikasi:
- Printer Thermal 58mm: Epson TM-T20, HOIN HOP-E58, Zjiang ZJ-5890K
- Printer Thermal 80mm: Epson TM-T82, HOIN HOP-E801, POS-5805DD

### Instalasi Driver
1. Unduh driver printer dari situs resmi produsen
2. Instal driver sesuai petunjuk instalasi
3. Pastikan printer terpasang sebagai printer default
4. Restart komputer setelah instalasi driver

### Konfigurasi Printer di Aplikasi
1. Buka aplikasi ParkingIN atau ParkingOut
2. Pilih menu **Pengaturan > Printer**
3. Pilih printer yang akan digunakan dari dropdown list
4. Sesuaikan ukuran kertas:
   - 58mm: Lebar 58mm, Panjang sesuai kebutuhan (biasanya 210mm)
   - 80mm: Lebar 80mm, Panjang sesuai kebutuhan (biasanya 210mm)
5. Klik "Tes Print" untuk memastikan pengaturan sudah benar
6. Simpan pengaturan

### Pengaturan Lanjutan Printer
Untuk mengakses pengaturan lanjutan printer:
1. Buka Control Panel > Devices and Printers
2. Klik kanan pada printer yang digunakan > Printing Preferences
3. Atur properti berikut:
   - Kualitas cetakan: Normal
   - Densitas: Medium
   - Kecepatan cetak: Normal
4. Klik OK untuk menyimpan pengaturan

### Troubleshooting Printer
| Masalah | Solusi |
|---------|--------|
| Printer tidak terdeteksi | - Periksa koneksi kabel<br>- Reinstall driver<br>- Restart aplikasi dan komputer |
| Hasil cetakan tidak jelas | - Periksa pengaturan densitas<br>- Bersihkan print head<br>- Ganti kertas thermal dengan kualitas lebih baik |
| Kertas macet | - Pastikan jalur kertas bersih<br>- Periksa pengaturan ukuran kertas<br>- Gunakan kertas yang direkomendasikan |
| Printer offline | - Cek status power dan koneksi<br>- Restart printer<br>- Set printer sebagai default |

## Konfigurasi Kamera

### Kamera yang Didukung
Sistem mendukung sebagian besar webcam USB standar dengan driver UVC (USB Video Class):
- Logitech C270, C310, C920
- Microsoft LifeCam
- Webcam built-in laptop

### Instalasi Kamera
1. Hubungkan kamera ke port USB komputer
2. Tunggu hingga Windows menginstal driver secara otomatis
3. Jika driver tidak terinstal otomatis, unduh dari situs produsen

### Konfigurasi Kamera di Aplikasi
1. Buka aplikasi ParkingIN atau ParkingOut
2. Pilih menu **Pengaturan > Kamera**
3. Pilih kamera dari dropdown list
4. Atur resolusi (direkomendasikan 640x480 atau 1280x720)
5. Atur framerate (direkomendasikan 15-30 fps)
6. Atur parameter gambar (brightness, contrast, saturation)
7. Klik "Test Camera" untuk melihat preview
8. Simpan pengaturan

### Penempatan Kamera
Untuk hasil optimal dalam menangkap plat nomor:
- Jarak: 1-2 meter dari kendaraan
- Sudut: Sejajar atau sedikit miring (15-30 derajat) terhadap plat nomor
- Tinggi: Sekitar 1-1.5 meter dari permukaan tanah
- Pencahayaan: Pastikan area cukup terang, tambahkan lampu jika diperlukan

### Troubleshooting Kamera
| Masalah | Solusi |
|---------|--------|
| Kamera tidak terdeteksi | - Periksa koneksi USB<br>- Coba port USB lain<br>- Reinstall driver kamera |
| Gambar gelap/blur | - Sesuaikan pengaturan brightness/contrast<br>- Pastikan pencahayaan cukup<br>- Periksa fokus kamera |
| Kamera freeze | - Tutup aplikasi lain yang mungkin menggunakan kamera<br>- Restart aplikasi<br>- Periksa kualitas kabel USB |
| Delay/lag video | - Kurangi resolusi atau framerate<br>- Pastikan CPU tidak overload<br>- Update driver kamera |

## Loop Detector dan Gate Controller

### Spesifikasi Loop Detector
- Tegangan kerja: 220 VAC atau 12-24 VDC (tergantung model)
- Output: Relay NO/NC
- Sensitivitas: Adjustable
- Frekuensi: 20-170 kHz
- Loop wire: 1.5mm² dengan minimal 3 putaran

### Konfigurasi Loop Detector
1. Pasang loop wire di bawah permukaan jalan dengan kedalaman 3-5 cm
2. Hubungkan ujung loop wire ke terminal loop detector
3. Atur sensitivitas loop detector (mulai dari level rendah dan tingkatkan secara bertahap)
4. Hubungkan output relay loop detector ke port serial komputer melalui konverter RS232-to-USB

### Spesifikasi Gate Controller
- Tegangan kerja: 220 VAC
- Motor: 90W-180W
- Waktu buka/tutup: 1-6 detik (adjustable)
- Mode kontrol: Manual dan otomatis
- Interface: Contact relay atau RS485

### Konfigurasi Gate Controller
1. Pasang gate controller sesuai petunjuk instalasi produsen
2. Hubungkan terminal kontrol ke port serial komputer melalui konverter RS232-to-USB
3. Atur waktu buka/tutup gate (direkomendasikan 3-4 detik)
4. Atur mode operasi ke mode remote control

### Konfigurasi Serial Port di Aplikasi
1. Buka aplikasi ParkingIN atau ParkingOut
2. Pilih menu **Pengaturan > Hardware**
3. Untuk Loop Detector:
   - Pilih COM Port yang terhubung ke loop detector
   - Set Baud Rate: 9600
   - Data Bits: 8
   - Parity: None
   - Stop Bits: 1
4. Untuk Gate Controller:
   - Pilih COM Port yang terhubung ke gate controller
   - Set Baud Rate: 9600 (atau sesuai spesifikasi gate controller)
   - Data Bits: 8
   - Parity: None
   - Stop Bits: 1
5. Klik "Tes Koneksi" untuk memastikan komunikasi berjalan baik
6. Simpan pengaturan

### Wiring Diagram
![Wiring Diagram](../Images/wiring_diagram.png)

## Mikrokontroler

### Mikrokontroler yang Didukung
Sistem dapat diintegrasikan dengan mikrokontroler:
- Arduino Uno/Mega
- ESP32/ESP8266
- Raspberry Pi Pico

### Koneksi Mikrokontroler
1. Hubungkan mikrokontroler ke komputer menggunakan kabel USB
2. Install driver jika diperlukan
3. Untuk Arduino/ESP32:
   - Upload firmware `ParkingSystem.ino` ke mikrokontroler
   - Firmware tersedia di folder `firmware/`
4. Konfigurasi koneksi di aplikasi (sama seperti konfigurasi serial port)

### Kode Firmware Dasar
Contoh kode dasar untuk Arduino:
```cpp
#define LOOP_SENSOR_PIN 2
#define GATE_OPEN_PIN 3
#define GATE_CLOSE_PIN 4
#define STATUS_LED_PIN 13

void setup() {
  Serial.begin(9600);
  pinMode(LOOP_SENSOR_PIN, INPUT_PULLUP);
  pinMode(GATE_OPEN_PIN, OUTPUT);
  pinMode(GATE_CLOSE_PIN, OUTPUT);
  pinMode(STATUS_LED_PIN, OUTPUT);
  
  digitalWrite(GATE_OPEN_PIN, LOW);
  digitalWrite(GATE_CLOSE_PIN, LOW);
  
  Serial.println("PARKING_CONTROLLER_READY");
}

void loop() {
  // Baca status sensor loop
  bool vehicleDetected = !digitalRead(LOOP_SENSOR_PIN);
  
  // Kirim status ke komputer
  if (vehicleDetected) {
    Serial.println("VEHICLE_DETECTED");
    digitalWrite(STATUS_LED_PIN, HIGH);
  } else {
    Serial.println("NO_VEHICLE");
    digitalWrite(STATUS_LED_PIN, LOW);
  }
  
  // Baca perintah dari komputer
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    if (command == "OPEN_GATE") {
      digitalWrite(GATE_OPEN_PIN, HIGH);
      delay(500);
      digitalWrite(GATE_OPEN_PIN, LOW);
      Serial.println("GATE_STATUS: OPENING");
      delay(3000);
      Serial.println("GATE_STATUS: OPEN");
    }
    else if (command == "CLOSE_GATE") {
      digitalWrite(GATE_CLOSE_PIN, HIGH);
      delay(500);
      digitalWrite(GATE_CLOSE_PIN, LOW);
      Serial.println("GATE_STATUS: CLOSING");
      delay(3000);
      Serial.println("GATE_STATUS: CLOSED");
    }
  }
  
  delay(100); // Jeda 100ms antara pembacaan
}
```

## Deteksi Sinyal dan Troubleshooting

### Deteksi Sinyal dari Mikrokontroler
Sistem secara otomatis mendeteksi sinyal yang dikirim oleh mikrokontroler. Berikut adalah cara kerjanya:

1. **Protokol Komunikasi**:
   - Komunikasi serial dengan format ASCII text
   - Setiap pesan diakhiri dengan karakter newline (`\n`)
   - Baud rate default: 9600bps

2. **Format Pesan dari Mikrokontroler**:
   - `VEHICLE_DETECTED`: Kendaraan terdeteksi oleh loop sensor
   - `NO_VEHICLE`: Tidak ada kendaraan terdeteksi
   - `GATE_STATUS: OPEN`: Gate terbuka
   - `GATE_STATUS: CLOSED`: Gate tertutup
   - `GATE_STATUS: OPENING`: Gate sedang membuka
   - `GATE_STATUS: CLOSING`: Gate sedang menutup

3. **Format Perintah ke Mikrokontroler**:
   - `OPEN_GATE`: Perintah membuka gate
   - `CLOSE_GATE`: Perintah menutup gate

4. **Penanganan Koneksi Terputus**:
   Aplikasi mengimplementasikan fitur watchdog yang akan mendeteksi jika mikrokontroler tidak mengirim data selama periode tertentu:
   
   ```csharp
   private void InitializeSerialWatchdog()
   {
       serialWatchdogTimer = new System.Threading.Timer(SerialWatchdogCallback, null, WATCHDOG_TIMEOUT_MS, Timeout.Infinite);
   }
   
   private void SerialWatchdogCallback(object state)
   {
       if (DateTime.Now - lastSerialDataTime > TimeSpan.FromMilliseconds(WATCHDOG_TIMEOUT_MS))
       {
           // Koneksi terputus
           InvokeOnMainThread(() => {
               UpdateUIStatus("Koneksi mikrokontroler terputus", Color.Red);
               LogWarning("Koneksi serial terputus - tidak ada data selama " + WATCHDOG_TIMEOUT_MS + "ms");
               
               // Coba hubungkan kembali
               CloseSerialConnection();
               InitializeSerialPorts();
           });
       }
       
       // Reset timer
       serialWatchdogTimer.Change(WATCHDOG_TIMEOUT_MS, Timeout.Infinite);
   }
   
   private void ProcessSerialData(string data)
   {
       lastSerialDataTime = DateTime.Now; // Update timestamp ketika data diterima
       // Proses data...
   }
   ```

5. **Indikator Status Koneksi**:
   - Aplikasi menampilkan indikator berwarna di sudut kanan bawah:
     - Hijau: Mikrokontroler terhubung dan berfungsi normal
     - Kuning: Mencoba menyambungkan kembali
     - Merah: Koneksi terputus

### Troubleshooting Koneksi Mikrokontroler

| Masalah | Solusi |
|---------|--------|
| Koneksi terputus | - Periksa kabel USB<br>- Pastikan mikrokontroler mendapat daya yang cukup<br>- Restart mikrokontroler<br>- Coba port USB yang berbeda |
| Data tidak diterima | - Periksa baud rate<br>- Pastikan format data sesuai<br>- Cek apakah ada perangkat lain yang menggunakan port serial tersebut |
| Gate tidak merespon | - Periksa koneksi ke relay<br>- Periksa tegangan motor gate<br>- Verifikasi perintah diterima oleh mikrokontroler dengan monitor serial |
| False detection | - Sesuaikan sensitivitas loop detector<br>- Periksa kualitas loop wire<br>- Jauhkan kabel loop dari sumber interferensi elektromagnetik |

### Mengatasi Masalah Interferensi
1. Gunakan kabel berpelindung (shielded cable) untuk koneksi loop detector
2. Pasang ferrite core pada kabel USB dan serial
3. Pisahkan jalur kabel power dan sinyal
4. Grounding yang baik untuk semua perangkat
5. Hindari memasang loop wire dekat dengan kabel listrik atau sumber interferensi lainnya

### Log dan Diagnostik
Semua komunikasi dengan mikrokontroler dicatat dalam file log:
- Lokasi: `logs/hardware_YYYYMMDD.log`
- Format log: `[Timestamp] [Level] [Component] Message`
- Contoh: `[2023-12-25 15:30:45] [INFO] [SerialPort] Received: VEHICLE_DETECTED`

Untuk melihat log secara realtime:
1. Buka aplikasi ParkingIN atau ParkingOut
2. Pilih menu **Tools > Serial Monitor**
3. Pilih port yang ingin dimonitor
4. Klik "Start Monitoring" 