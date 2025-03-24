# ParkIRC - Sistem Manajemen Parkir Modern

ParkIRC adalah sistem manajemen parkir modern yang menggabungkan otomatisasi di pintu masuk dan verifikasi operator di pintu keluar untuk pengalaman parkir yang efisien dan aman.

## Fitur Utama

### 1. Pintu Masuk (Full Otomatis)
- Operasi dipicu oleh push button yang ditekan pengguna
- Deteksi otomatis kendaraan dan plat nomor menggunakan kamera
- Cetak tiket dengan barcode secara otomatis
- Gate terbuka otomatis setelah tiket diambil
- Pencatatan otomatis data masuk ke database (waktu, foto, plat nomor)

### 2. Pintu Keluar (Operator)
- Scan barcode tiket oleh operator
- Perhitungan biaya otomatis berdasarkan durasi
- Verifikasi pembayaran oleh operator
- Cetak struk pembayaran
- Gate terbuka setelah pembayaran selesai

### 3. Monitoring Real-time
- Status pintu masuk dan keluar
- Status perangkat (kamera, printer, push button)
- Riwayat transaksi terkini
- Riwayat kendaraan dengan filter:
  - Status (Masuk/Keluar)
  - Rentang waktu
  - Jenis kendaraan
  - Plat nomor
- Notifikasi kejadian penting
- Monitoring jumlah kendaraan

### 4. Manajemen
- Kelola slot parkir
- Kelola operator dan shift
- Konfigurasi tarif parkir
- Pengaturan kamera dan printer
- Riwayat lengkap kendaraan:
  - Pencarian berdasarkan status masuk/keluar
  - Filter berdasarkan tanggal
  - Export data ke Excel/PDF
  - Detail transaksi per kendaraan
- Laporan dan statistik

## Teknologi

- **Backend**: ASP.NET Core 6.0
- **Frontend**: Bootstrap 5, jQuery
- **Database**: PostgreSQL
- **Real-time**: SignalR
- **Hardware Integration**:
  - Kamera IP untuk ANPR (Automatic Number Plate Recognition)
  - Thermal printer untuk tiket dan struk
  - Push button untuk trigger pintu masuk
  - Gate controller untuk palang pintu
  - Barcode scanner di pintu keluar

## Persyaratan Sistem

- .NET 6.0 SDK
- PostgreSQL 14+
- Apache (untuk production)
- Modern web browser
- Perangkat keras yang kompatibel

## Instalasi

1. Clone repository
```bash
git clone https://github.com/yourusername/ParkIRC.git
cd ParkIRC
```

2. Restore dependencies
```bash
dotnet restore
```

3. Update database
```bash
dotnet ef database update
```

4. Setup konfigurasi
- Copy `appsettings.example.json` ke `appsettings.json`
- Sesuaikan koneksi database
- Konfigurasi printer dan kamera
- Setup email untuk notifikasi

5. Jalankan aplikasi
```bash
dotnet run
```

## Konfigurasi Hardware

### Pintu Masuk
1. Setup kamera IP
   - Konfigurasi di `CameraSettings`
   - Pastikan posisi optimal untuk ANPR
   
2. Koneksi push button
   - Ikuti panduan di `manualbook/mikrocontroller_avr_setup.md`
   - Test koneksi menggunakan simulator

3. Setup printer tiket
   - Ikuti langkah di `setup-printer.sh`
   - Test cetak tiket

### Pintu Keluar
1. Koneksi barcode scanner
   - Konfigurasi sebagai keyboard emulation
   - Test pembacaan tiket

2. Setup printer struk
   - Konfigurasi di `PrinterSettings`
   - Test cetak struk

## Dokumentasi

- [Panduan Administrator](manualbook/administrator_server_guide.md)
- [Setup Mikrokontroler](manualbook/mikrocontroller_avr_setup.md)
- [Database Real-time](manualbook/realtime_database.md)
- [Alur Sistem](system_flow.md)
- [Dokumentasi Teknis Lengkap](readmeplus.md)
- [Manual Penggunaan](ParkIRC-Manual.md)

## Kontribusi

1. Fork repository
2. Buat branch fitur (`git checkout -b feature/AmazingFeature`)
3. Commit perubahan (`git commit -m 'Add some AmazingFeature'`)
4. Push ke branch (`git push origin feature/AmazingFeature`)
5. Buat Pull Request

## Lisensi

Distributed under the MIT License. See `LICENSE` for more information.

## Kontak

Your Name - [@yourtwitter](https://twitter.com/yourtwitter) - email@example.com

Project Link: [https://github.com/yourusername/ParkIRC](https://github.com/yourusername/ParkIRC)
