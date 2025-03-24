# ParkIRC - Parking Management System

ParkIRC adalah sistem manajemen parkir modern yang dibangun menggunakan .NET Core 6.0. Sistem ini menyediakan solusi lengkap untuk mengelola area parkir, termasuk manajemen kendaraan, tiket parkir, dan laporan.

## Fitur Utama

- Manajemen Parkir Kendaraan
- Sistem Tiket dengan QR Code
- Manajemen Operator/Staff
- Dashboard Real-time
- Laporan dan Analitik
- Sistem Multi-user dengan Role-based Access Control

## Persyaratan Sistem

- Windows 64-bit
- .NET 6.0 Runtime
- SQLite
- Port 5126 dan 5127 tersedia
- Minimal RAM 4GB
- Ruang disk minimal 500MB

## Instalasi

### Menggunakan Installer

1. Download file installer (.msi) dari [releases page](https://github.com/yourusername/ParkIRC/releases)
2. Jalankan installer
3. Ikuti langkah-langkah instalasi
4. Aplikasi akan otomatis terbuka setelah instalasi selesai

### Manual Installation

1. Download file zip dari releases
2. Extract ke folder yang diinginkan
3. Jalankan `start.bat`

## Penggunaan

### Login Default

- Email: admin@parkingsystem.com
- Password: Admin@123

menjalankan : cd /home/idiarso/Desktop/APP/PARKIR_WEB-main && dotnet run --project=ParkIRC.csproj

### Mengakses Aplikasi

1. Buka browser
2. Akses http://localhost:5126 atau https://localhost:5127
3. Login menggunakan kredensial di atas

## Pengaturan Kamera dan ROI

### Konfigurasi Dasar
Sistem menggunakan pengaturan kamera yang dapat disesuaikan untuk kondisi pencahayaan berbeda. File konfigurasi berada di `appsettings.json`:

```json
{
  "CameraSettings": {
    "Resolution": {
      "Width": 1280,
      "Height": 720
    },
    "ROI": {
      "X": 0,
      "Y": 0,
      "Width": 640,
      "Height": 480
    }
  }
}
```

### Optimasi untuk Kondisi Pencahayaan Berbeda

#### 1. Kondisi Pencahayaan Rendah (Malam/Indoor)
```json
{
  "CameraSettings": {
    "LowLight": {
      "Exposure": 150,     // Nilai 100-200
      "Gain": 2.0,        // Nilai 1.5-2.5
      "Brightness": 60,    // Nilai 55-65
      "Contrast": 55      // Nilai 50-60
    }
  }
}
```

#### 2. Kondisi Pencahayaan Tinggi (Siang/Outdoor)
```json
{
  "CameraSettings": {
    "BrightLight": {
      "Exposure": 50,      // Nilai 30-70
      "Gain": 1.0,        // Nilai 1.0-1.2
      "Brightness": 40,    // Nilai 35-45
      "Contrast": 45      // Nilai 40-50
    }
  }
}
```

### Tips Pengaturan ROI dengan Pencahayaan Berbeda

1. **Kondisi Gelap**:
   - Perbesar area ROI untuk menangkap lebih banyak cahaya
   - Tingkatkan Exposure dan Gain
   - Gunakan nilai Brightness yang lebih tinggi
   - Pastikan ada pencahayaan tambahan di area scan

2. **Kondisi Terang**:
   - Area ROI bisa lebih kecil dan fokus
   - Kurangi Exposure untuk menghindari overexposure
   - Turunkan Gain untuk mengurangi noise
   - Sesuaikan Contrast untuk ketajaman gambar

3. **Optimasi Performa**:
   - Mulai dengan area ROI yang kecil
   - Sesuaikan ukuran ROI jika hasil tidak optimal
   - Monitor CPU usage saat penyesuaian
   - Test di berbagai kondisi pencahayaan

### Troubleshooting

1. **Gambar Terlalu Gelap**:
   - Tingkatkan Exposure (+=50)
   - Naikkan Brightness (+=10)
   - Tambah Gain (+0.5)

2. **Gambar Terlalu Terang**:
   - Kurangi Exposure (-=50)
   - Turunkan Brightness (-=10)
   - Kurangi Gain (-0.5)

3. **Gambar Blur**:
   - Pastikan fokus kamera tepat
   - Sesuaikan area ROI
   - Tingkatkan Contrast
   - Kurangi Gain

4. **CPU Usage Tinggi**:
   - Kecilkan area ROI
   - Turunkan resolusi
   - Kurangi framerate
   - Monitor penggunaan memory

### Rekomendasi Hardware
- Kamera: Minimal 720p dengan manual focus
- Processor: Intel i3/AMD Ryzen 3 atau lebih tinggi
- RAM: Minimal 4GB
- Storage: SSD direkomendasikan

## Dokumentasi Lengkap
Untuk panduan lengkap, silakan lihat:
- [Manual Book](docs/ParkIRC-Manual.pdf)
- [Quick Guide](docs/ParkIRC-Quick-Guide.pdf)
- [Screenshot Guide](docs/screenshot-guide.md)

## Pengembangan

### Prerequisites

- Visual Studio 2022
- .NET 6.0 SDK
- WiX Toolset v3.14.1 (untuk membuat installer)

### Build dari Source

1. Clone repository
```bash
git clone https://github.com/yourusername/ParkIRC.git
```

2. Buka solution di Visual Studio
```bash
cd ParkIRC
start ParkIRC.sln
```

3. Restore packages
```bash
dotnet restore
```

4. Build project
```bash
dotnet build
```

5. Run aplikasi
```bash
dotnet run
```

## Kontribusi

Silakan berkontribusi dengan membuat pull request. Untuk perubahan major, harap buka issue terlebih dahulu untuk mendiskusikan perubahan yang diinginkan.

## Lisensi

[MIT License](LICENSE)

## Kontak

- Email: your.email@example.com
- Website: https://yourwebsite.com
- Issue Tracker: https://github.com/yourusername/ParkIRC/issues
