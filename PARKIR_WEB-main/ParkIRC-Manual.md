# Manual Book ParkIRC - Sistem Manajemen Parkir

## Daftar Isi
1. Pendahuluan
2. Instalasi Sistem
3. Login dan Autentikasi
4. Dashboard
5. Manajemen Parkir
6. Manajemen Operator
7. Laporan dan Analitik
8. Pengaturan Sistem
9. Troubleshooting
10. Perawatan Database dan Sistem

## 1. Pendahuluan

### 1.1 Tentang ParkIRC
ParkIRC adalah sistem manajemen parkir modern yang dirancang untuk memudahkan pengelolaan area parkir. Sistem ini menyediakan fitur lengkap untuk manajemen kendaraan, tiket parkir, operator, dan pelaporan.

### 1.2 Fitur Utama
- Manajemen Parkir Kendaraan
- Sistem Tiket dengan QR Code
- Manajemen Operator/Staff
- Dashboard Real-time
- Laporan dan Analitik
- Sistem Multi-user dengan Role-based Access Control

### 1.3 Persyaratan Sistem
- Windows 64-bit atau Linux 64-bit
- RAM minimal 4GB (direkomendasikan 8GB)
- Ruang disk minimal 500MB
- Port 5126 dan 5127 tersedia
- Browser modern (Chrome, Firefox, Edge)

## 2. Instalasi Sistem

### 2.1 Instalasi di Windows
1. Extract file ParkIRC-Windows-v1.0.0.zip
2. Jalankan start.bat
3. Akses http://localhost:5126 di browser

### 2.2 Instalasi di Linux
1. Extract file ParkIRC-Linux-v1.0.0.zip
2. Buat file start.sh executable: `chmod +x start.sh`
3. Jalankan ./start.sh
4. Akses http://localhost:5126 di browser

## 3. Login dan Autentikasi

### 3.1 Akses Pertama Kali
- Email default: admin@parkingsystem.com
- Password default: Admin@123

### 3.2 Mengubah Password
1. Klik profil di pojok kanan atas
2. Pilih "Change Password"
3. Masukkan password lama dan password baru
4. Klik "Save"

### 3.3 Role dan Hak Akses
- Admin: Akses penuh ke semua fitur
- Staff: Akses terbatas untuk operasi parkir

## 4. Dashboard

### 4.1 Komponen Dashboard
- Total Kendaraan Terparkir
- Slot Parkir Tersedia
- Pendapatan Hari Ini
- Grafik Okupansi
- Aktivitas Terkini

### 4.2 Fitur Real-time
- Update otomatis status parkir
- Notifikasi kendaraan masuk/keluar
- Alert slot penuh

## 5. Manajemen Parkir

### 5.1 Kendaraan Masuk
1. Klik "Kendaraan Masuk"
2. Isi informasi kendaraan:
   - Nomor Plat
   - Jenis Kendaraan
   - Foto Kendaraan (opsional)
3. Sistem akan:
   - Generate QR Code
   - Assign slot parkir
   - Catat waktu masuk

### 5.2 Kendaraan Keluar
1. Scan QR Code atau masukkan nomor tiket
2. Sistem akan:
   - Hitung durasi parkir
   - Kalkulasi biaya
   - Generate struk
3. Proses pembayaran
4. Konfirmasi keluar

### 5.3 Manajemen Slot Parkir
- Tambah/Edit slot parkir
- Set tarif per slot
- Monitoring status slot
- Blokir slot untuk maintenance

## 6. Manajemen Operator

### 6.1 Tambah Operator Baru
1. Menu "Operators" > "Add New"
2. Isi informasi:
   - Nama Lengkap
   - Email
   - Role
   - Shift
3. Sistem akan mengirim kredensial via email

### 6.2 Manajemen Shift
- Atur jadwal shift
- Rotasi operator
- Monitor kehadiran

## 7. Laporan dan Analitik

### 7.1 Laporan Harian
- Total kendaraan
- Pendapatan
- Durasi rata-rata
- Okupansi

### 7.2 Laporan Keuangan
- Pendapatan per periode
- Breakdown per jenis kendaraan
- Analisis trend

### 7.3 Export Data
- Format: PDF, Excel, CSV
- Filter berdasarkan periode
- Custom report

## 8. Pengaturan Sistem

### 8.1 Konfigurasi Umum
- Nama lokasi parkir
- Zona waktu
- Format tanggal/waktu
- Logo dan branding

### 8.2 Pengaturan Tarif
- Tarif dasar
- Tarif per jam
- Tarif maksimal
- Diskon dan promo

### 8.3 Backup & Restore
- Backup otomatis database
- Restore dari backup
- Export konfigurasi

## 9. Troubleshooting

### 9.1 Masalah Umum
1. Aplikasi tidak bisa diakses:
   - Cek status service
   - Verifikasi port
   - Periksa firewall

2. Printer tidak berfungsi:
   - Cek koneksi printer
   - Verifikasi driver
   - Test print

3. QR Code tidak terbaca:
   - Bersihkan scanner
   - Cek pencahayaan
   - Generate ulang QR

### 9.2 Log dan Monitoring
- Lokasi file log
- Monitoring performa
- Alert dan notifikasi

### 9.3 Kontak Support
- Email: support@parkirc.com
- Telepon: (021) xxx-xxxx
- Jam layanan: 24/7

## 10. Perawatan Database dan Sistem

### 10.1 Manajemen Database
1. Backup Database SQLite:
```bash
# Windows
xcopy GeexParking.db backup\GeexParking_%date:~-4,4%%date:~-10,2%%date:~-7,2%.db

# Linux
cp GeexParking.db backup/GeexParking_$(date +%Y%m%d).db
```

2. Compress Database:
```bash
# Jalankan vacuum untuk mengoptimalkan ukuran
sqlite3 GeexParking.db "VACUUM;"
```

3. Maintenance Database:
```sql
-- Hapus data parkir lama (lebih dari 6 bulan)
DELETE FROM ParkingTransactions WHERE ExitTime < date('now', '-6 months');

-- Hapus log aktivitas lama
DELETE FROM Journals WHERE Timestamp < date('now', '-3 months');

-- Optimize indexes
REINDEX;
```

4. Monitoring Ukuran:
```bash
# Windows
dir GeexParking.db

# Linux
ls -lh GeexParking.db
```

### 10.2 Backup Otomatis

#### Windows (backup.bat)
```batch
@echo off
set BACKUP_DIR=backup
set DB_FILE=GeexParking.db
set TIMESTAMP=%date:~-4,4%%date:~-10,2%%date:~-7,2%

if not exist %BACKUP_DIR% mkdir %BACKUP_DIR%
xcopy %DB_FILE% %BACKUP_DIR%\%DB_FILE%_%TIMESTAMP%.db
forfiles /P %BACKUP_DIR% /M *.db /D -30 /C "cmd /c del @path"
```

#### Linux (backup.sh)
```bash
#!/bin/bash
BACKUP_DIR="backup"
DB_FILE="GeexParking.db"
TIMESTAMP=$(date +%Y%m%d)

mkdir -p $BACKUP_DIR
cp $DB_FILE $BACKUP_DIR/${DB_FILE}_${TIMESTAMP}.db
find $BACKUP_DIR -name "*.db" -mtime +30 -delete
```

### 10.3 Restore Database
1. Stop aplikasi
2. Backup database current
3. Copy file backup:
```bash
# Windows
copy backup\GeexParking_20240309.db GeexParking.db

# Linux
cp backup/GeexParking_20240309.db GeexParking.db
```
4. Restart aplikasi

### 10.4 Pengaturan Kamera

#### Konfigurasi Intensitas Kamera
1. Buka file `appsettings.json`
2. Cari bagian "CameraSettings":
```json
{
  "CameraSettings": {
    "Resolution": {
      "Width": 1280,
      "Height": 720
    },
    "Exposure": {
      "Auto": false,
      "Value": 100  // 0-200
    },
    "Brightness": 50,  // 0-100
    "Contrast": 50,    // 0-100
    "ROI": {
      "X": 0,
      "Y": 0,
      "Width": 640,
      "Height": 480
    },
    "CaptureSettings": {
      "LowLight": {
        "Exposure": 150,
        "Gain": 2.0,
        "Brightness": 60
      },
      "BrightLight": {
        "Exposure": 50,
        "Gain": 1.0,
        "Brightness": 40
      }
    }
  }
}
```

#### Optimasi ROI dan Intensitas
1. Pengaturan ROI (Region of Interest):
   - Sesuaikan X, Y untuk posisi area scan
   - Atur Width, Height sesuai area plat nomor
   - Semakin kecil area, semakin cepat proses

2. Pengaturan Cahaya:
   - LowLight: untuk kondisi gelap/malam
   - BrightLight: untuk kondisi terang/siang
   - Exposure: waktu tangkap cahaya (ms)
   - Gain: penguatan sinyal cahaya
   - Brightness: kecerahan gambar

3. Tips Optimasi:
   - Kondisi Gelap:
     * Tingkatkan Exposure (100-150)
     * Naikkan Gain (1.5-2.0)
     * Tambah Brightness (55-65)
   - Kondisi Terang:
     * Kurangi Exposure (30-50)
     * Turunkan Gain (1.0)
     * Kurangi Brightness (35-45)

## Lampiran

### A. Shortcut Keyboard
- F1: Help
- F2: Kendaraan Masuk
- F3: Kendaraan Keluar
- F4: Cari Kendaraan
- F5: Refresh Dashboard

### B. Format Tiket
- Header: Nama lokasi
- QR Code
- Nomor plat
- Waktu masuk
- Slot parkir
- Terms & Conditions

### C. Maintenance Rutin
- Backup database (harian)
- Cek printer (mingguan)
- Update sistem (bulanan)
- Pembersihan data (3 bulan)

### D. Keamanan
- Password policy
- Session timeout
- IP whitelist
- Audit trail 