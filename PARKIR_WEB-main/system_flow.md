# Diagram Alur Sistem Parkir

## Komponen Sistem

```
+------------------+      +------------------+      +------------------+
|                  |      |                  |      |                  |
|    ParkingIN     |<---->|  ParkingServer   |<---->|    ParkingOUT    |
|    (Entry)       |      | (WebSocket/DB)   |      |    (Exit)        |
|                  |      |                  |      |                  |
+--------^---------+      +------------------+      +--------^---------+
         |                                                   |
         |                                                   |
+--------v---------+                                +--------v---------+
|                  |                                |                  |
| Mikrokontroler   |                                | Mikrokontroler   |
|    ATMEL         |                                |    ATMEL         |
| (Entry Gate)     |                                | (Exit Gate)      |
|                  |                                |                  |
+--------^---------+                                +--------^---------+
         |                                                   |
         |                                                   |
+--------v---------+      +------------------+      +--------v---------+
|                  |      |                  |      |                  |
|  Push Button     |      |   PostgreSQL     |      |  Barcode Scanner |
|  & Sensor Masuk  |      |    Database      |      |  & Printer Keluar|
|                  |      |                  |      |                  |
+--------^---------+      +------------------+      +--------^---------+
         |                                                   |
         |                                                   |
+--------v---------+                                +--------v---------+
|                  |                                |                  |
|    Kamera        |                                |  Gate Barrier    |
|  (Entry Point)   |                                |  (Exit Point)    |
|                  |                                |                  |
+------------------+                                +------------------+
```

## Alur Proses Sistem

### Proses Masuk (Entry)

```
+---------------+     +---------------+     +---------------+
| Kendaraan     |     | Push Button   |     | ATMEL MCU     |
| Terdeteksi    |---->| Ditekan       |---->| Kirim Sinyal  |
|               |     |               |     | "IN:<ID>"     |
+---------------+     +---------------+     +-------+-------+
                                                    |
                                                    v
+---------------+     +---------------+     +---------------+
| Gambar        |     | Data          |     | ParkingIN     |
| Disimpan      |<----| Diproses      |<----| Terima Sinyal |
|               |     |               |     |               |
+-------+-------+     +-------+-------+     +---------------+
        |                     |
        v                     v
+---------------+     +---------------+     +---------------+
| Tiket Dengan  |     | Portal        |     | Kendaraan     |
| Barcode       |---->| Terbuka       |---->| Masuk         |
| Dicetak       |     |               |     |               |
+---------------+     +---------------+     +---------------+
```

### Proses Keluar (Exit)

```
+---------------+     +---------------+     +---------------+
| Kendaraan     |     | Barcode Tiket |     | ParkingOUT    |
| di Exit Gate  |---->| Dipindai      |---->| Proses Data   |
|               |     |               |     |               |
+---------------+     +---------------+     +-------+-------+
                                                    |
                                                    v
+---------------+     +---------------+     +---------------+
| Petugas       |     | Sistem        |     | Data Parkir   |
| Verifikasi    |<----| Tampilkan     |<----| & Foto        |
| Kendaraan     |     | Foto Entry    |     | Diambil       |
+-------+-------+     +---------------+     +---------------+
        |
        v
+---------------+     +---------------+     +---------------+
| Transaksi     |     | Tombol        |     | Sinyal Buka   |
| Pembayaran    |---->| "Buka Gate"   |---->| Dikirim ke MCU|
| (jika ada)    |     | Ditekan       |     |               |
+---------------+     +---------------+     +-------+-------+
                                                    |
                                                    v
+---------------+     +---------------+     +---------------+
| Struk         |     | Portal Exit   |     | Kendaraan     |
| Pembayaran    |---->| Terbuka       |---->| Keluar        |
| Dicetak       |     |               |     |               |
+---------------+     +---------------+     +---------------+
```

## Detail Alur Proses Hardware

### 1. Proses Masuk Kendaraan (Entry)

1. **Trigger Kamera:**
   - Driver kendaraan menekan push button pada gate masuk
   - Push button terhubung ke ATMEL MCU
   - MCU mengirim sinyal melalui port serial (RS232/DB9) ke PC dengan format "IN:<timestamp>"
   - HardwareManager.cs di aplikasi ParkingIN menerima sinyal dan memicu proses berikutnya

2. **Pengambilan Gambar:**
   - ParkingEntryHandler.cs menerima sinyal trigger
   - `HardwareManager.CaptureEntryImageAsync()` dipanggil untuk mengambil gambar
   - Kamera (webcam atau IP) mengambil gambar kendaraan
   - Gambar disimpan dengan format ID tiket: `YYYYMMDD_HHMMSS_<random>.jpg`
   - Path gambar disimpan di database untuk referensi nanti

3. **Pencetakan Tiket:**
   - `HardwareManager.PrintTicket()` dipanggil dengan data tiket
   - Tiket dicetak dengan printer thermal yang terkonfigurasi
   - Format tiket sesuai konfigurasi di `printer.ini` termasuk:
     - Barcode dengan format Code128 atau QR
     - Waktu masuk dan informasi lokasi parkir
     - Tarif dasar (jika diaplikasikan)

4. **Pembukaan Gate:**
   - `HardwareManager.OpenEntryGate()` dipanggil untuk membuka portal
   - Sinyal dikirim melalui port serial ke ATMEL MCU
   - MCU mengaktifkan relay untuk membuka gate barrier
   - Sensor keamanan memantau area gate
   - Setelah durasi timeout, gate ditutup kembali secara otomatis

### 2. Proses Keluar Kendaraan (Exit)

1. **Pemindaian Barcode:**
   - Petugas atau pengemudi memindai barcode tiket dengan barcode scanner
   - Scanner (terhubung sebagai HID keyboard) mengirim data barcode + Enter
   - ParkingExitHandler.cs menerima data barcode
   - `ParkingExitHandler.ProcessBarcodeData()` memproses data barcode

2. **Verifikasi Kendaraan:**
   - Data kendaraan diambil dari database berdasarkan ID barcode
   - Foto kendaraan saat masuk ditampilkan di layar
   - Petugas memverifikasi bahwa kendaraan yang keluar sesuai dengan foto
   - Jika diperlukan, petugas dapat menambahkan catatan atau override tarif

3. **Kalkulasi Biaya:**
   - Sistem menghitung durasi parkir (waktu keluar - waktu masuk)
   - Tarif dihitung berdasarkan jenis kendaraan dan durasi
   - Jika pembayaran diperlukan, petugas memproses pembayaran
   - Data pembayaran dicatat dalam database

4. **Pencetakan Struk:**
   - `HardwareManager.PrintReceipt()` dipanggil dengan data pembayaran
   - Struk dicetak menggunakan printer thermal
   - Struk berisi rincian parkir dan pembayaran

5. **Pembukaan Gate:**
   - Setelah verifikasi dan pembayaran, petugas menekan tombol "Buka Gate"
   - `HardwareManager.OpenExitGate()` dipanggil untuk membuka portal keluar
   - Sinyal dikirim melalui port serial ke ATMEL MCU
   - MCU mengaktifkan relay untuk membuka gate barrier
   - Status transaksi diupdate menjadi "Completed" di database

### 3. Diagram Sequence Detail Kamera

```
+-------------------+  +-------------------+  +-------------------+  +-------------------+
| Push Button/MCU   |  | HardwareManager   |  | Camera System     |  | Storage System    |
+-------------------+  +-------------------+  +-------------------+  +-------------------+
         |                      |                      |                      |
         | Serial Signal        |                      |                      |
         |--------------------->|                      |                      |
         |                      | Initialize Camera    |                      |
         |                      |--------------------->|                      |
         |                      |                      |                      |
         |                      | Capture Image        |                      |
         |                      |--------------------->|                      |
         |                      |                      |                      |
         |                      |     Image Data       |                      |
         |                      |<---------------------|                      |
         |                      |                      |                      |
         |                      | Save Image           |                      |
         |                      |--------------------------------------------->|
         |                      |                      |                      |
         |                      |           File Path                         |
         |                      |<---------------------------------------------|
         |                      |                      |                      |
```

### 4. Diagram Sequence Detail Gate Control

```
+-------------------+  +-------------------+  +-------------------+  +-------------------+
| UI/Operator       |  | HardwareManager   |  | Serial Port       |  | ATMEL MCU         |
+-------------------+  +-------------------+  +-------------------+  +-------------------+
         |                      |                      |                      |
         | Open Gate Request    |                      |                      |
         |--------------------->|                      |                      |
         |                      | Initialize Port      |                      |
         |                      |--------------------->|                      |
         |                      |                      |                      |
         |                      | Send Command         |                      |
         |                      |--------------------->|                      |
         |                      |                      | Serial Data          |
         |                      |                      |--------------------->|
         |                      |                      |                      |
         |                      |                      |                      | Activate Relay
         |                      |                      |                      |----------+
         |                      |                      |                      |          |
         |                      |                      | Acknowledgement      |          |
         |                      |                      |<---------------------|          |
         |                      |  Command Result      |                      |          |
         |                      |<---------------------|                      |          |
         |  Gate Status         |                      |                      |          |
         |<---------------------|                      |                      |          |
         |                      |                      |                      |          |
         |                      |                      |                      | Gate Opens
         |                      |                      |                      |<---------+
         |                      |                      |                      |
```

### 5. Diagram Sequence Detail Printer

```
+-------------------+  +-------------------+  +-------------------+  +-------------------+
| ParkingHandler    |  | HardwareManager   |  | Printer System    |  | User/Operator     |
+-------------------+  +-------------------+  +-------------------+  +-------------------+
         |                      |                      |                      |
         | Print Request        |                      |                      |
         |--------------------->|                      |                      |
         |                      | Generate Ticket Data |                      |
         |                      |----------+           |                      |
         |                      |          |           |                      |
         |                      |<---------+           |                      |
         |                      |                      |                      |
         |                      | Print Command        |                      |
         |                      |--------------------->|                      |
         |                      |                      |                      |
         |                      |                      | Printing...          |
         |                      |                      |----------+           |
         |                      |                      |          |           |
         |                      |                      |<---------+           |
         |                      |                      |                      |
         |                      |  Print Status        |                      |
         |                      |<---------------------|                      |
         |  Print Complete      |                      |                      |
         |<---------------------|                      |                      |
         |                      |                      | Ticket/Receipt       |
         |                      |                      |--------------------->|
         |                      |                      |                      |
```

## Komunikasi antar Hardware

### 1. Komunikasi Serial (RS232/DB9)

Komunikasi antara PC dan ATMEL MCU menggunakan format berikut:

#### Dari MCU ke PC:
- `IN:<timestamp>` - Sinyal dari push button entry
- `STATUS:READY` - MCU siap menerima perintah
- `STATUS:BUSY` - MCU sedang memproses perintah
- `ERROR:<error_code>` - Error pada MCU

#### Dari PC ke MCU:
- `OPEN_ENTRY` - Perintah untuk membuka gate masuk
- `CLOSE_ENTRY` - Perintah untuk menutup gate masuk
- `OPEN_EXIT` - Perintah untuk membuka gate keluar
- `CLOSE_EXIT` - Perintah untuk menutup gate keluar
- `STATUS` - Request status MCU

### 2. Komunikasi Database
Data transaksi parkir disimpan dalam database PostgreSQL dengan struktur:

```sql
-- Contoh kueri untuk mendapatkan data ticket berdasarkan barcode
SELECT v.vehicle_id, v.entry_time, v.entry_image, v.ticket_number, v.vehicle_type
FROM vehicles v
WHERE v.ticket_number = '{barcode}'
AND v.exit_time IS NULL;

-- Contoh kueri untuk update saat kendaraan keluar
UPDATE vehicles
SET exit_time = CURRENT_TIMESTAMP, 
    exit_image = '{imagePath}', 
    parking_fee = {fee}, 
    payment_status = 'PAID'
WHERE ticket_number = '{barcode}';
```

### 3. Komunikasi Kamera

#### Webcam:
- Menggunakan library AForge.Video.DirectShow
- Menangkap frame melalui event handler
- Menyimpan gambar dalam format JPG

#### IP Camera:
- Menggunakan HTTP request ke snapshot URL
- Format URL: `http://{ip}:{port}/snapshot.jpg` atau API khusus vendor
- Autentikasi menggunakan Basic Auth (username/password)

## Konfigurasi Hardware

Semua pengaturan hardware tersimpan dalam file konfigurasi terpisah:

1. **camera.ini** - Konfigurasi kamera (IP/Webcam)
2. **gate.ini** - Konfigurasi gate controller dan serial port
3. **printer.ini** - Konfigurasi printer thermal dan format tiket
4. **network.ini** - Konfigurasi database dan koneksi jaringan

File-file ini dikelola melalui `HardwareManager.cs` yang berfungsi sebagai interface terpadu untuk semua komponen hardware.

## Rincian Komponen

### Hardware
- **PC Server**: Menjalankan PostgreSQL dan ParkingServer
- **PC Entry**: Menjalankan ParkingIN
- **PC Exit**: Menjalankan ParkingOUT
- **ATMEL MCU**: Mikrokontroler untuk komunikasi dengan push button dan gate barrier
- **Printer Thermal**: Untuk mencetak tiket dan struk
- **Kamera**: Webcam atau Kamera IP untuk pengambilan gambar kendaraan
- **Barcode Scanner**: Untuk memindai tiket masuk
- **Push Button**: Memicu proses cetak tiket dan pengambilan gambar
- **Gate Barrier**: Portal otomatis untuk entry dan exit

### Software
- **ParkingIN**: Aplikasi untuk mengelola proses masuk kendaraan
- **ParkingOUT**: Aplikasi untuk mengelola proses keluar kendaraan
- **ParkingServer**: WebSocket server untuk komunikasi real-time
- **PostgreSQL**: Database untuk menyimpan data transaksi, pengguna, dan log

## Alur Data
1. Data kendaraan masuk disimpan di database dengan ID unik
2. Foto kendaraan disimpan dengan nama sesuai ID unik
3. ID unik dicetak dalam bentuk barcode pada tiket
4. Saat kendaraan keluar, data diambil berdasarkan ID dari barcode
5. Foto kendaraan saat masuk ditampilkan untuk verifikasi
6. Setelah verifikasi, gate dibuka dan data transaksi diupdate
