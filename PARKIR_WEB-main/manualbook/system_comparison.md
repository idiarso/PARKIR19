# Perbandingan Sistem: ParkingIN vs IUTVehicleManager

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Ringkasan Perbedaan](#ringkasan-perbedaan)
3. [Perbedaan Fitur Utama](#perbedaan-fitur-utama)
4. [Perbedaan Arsitektur](#perbedaan-arsitektur)
5. [Perbedaan Database](#perbedaan-database)
6. [Perbedaan UI dan UX](#perbedaan-ui-dan-ux)
7. [Perbedaan Implementasi Hardware](#perbedaan-implementasi-hardware)
8. [Rekomendasi Penggunaan](#rekomendasi-penggunaan)

## Pendahuluan

Dokumen ini menjelaskan perbedaan antara dua sistem manajemen parkir yang ada di lingkungan:

1. **ParkingIN/ParkingOut**: Sistem manajemen parkir yang terdiri dari aplikasi masuk (ParkingIN) dan aplikasi keluar (ParkingOut) terpisah.

2. **IUTVehicleManager**: Sistem manajemen kendaraan terintegrasi yang menangani semua aspek (masuk, keluar, administrasi) dalam satu platform.

Kedua sistem ini memiliki tujuan yang serupa (mengelola kendaraan yang masuk dan keluar area parkir), namun dengan pendekatan implementasi, arsitektur, dan fitur yang berbeda.

## Ringkasan Perbedaan

| Aspek | ParkingIN/ParkingOut | IUTVehicleManager |
|-------|----------------------|-------------------|
| **Arsitektur** | Sistem terpisah untuk masuk dan keluar | Sistem terintegrasi (all-in-one) |
| **Versi .NET** | .NET 6.0/.NET 8.0 | .NET 6.0 |
| **Framework UI** | Windows Forms | Windows Forms (modernized) |
| **Database** | MySQL | Microsoft SQL Server |
| **Integrasi Hardware** | Terpisah per aplikasi | Terpusat |
| **Fokus Penggunaan** | Operator pos masuk/keluar | Manajemen kendaraan menyeluruh |
| **Mode Offline** | Didukung dengan sinkronisasi | Didukung dengan pendekatan berbeda |
| **Deployment** | Terpisah | Terpusat dengan konfigurasi mode |

## Perbedaan Fitur Utama

### ParkingIN/ParkingOut
- **Fokus pada Proses**: Dirancang khusus untuk menangani proses masuk (ParkingIN) dan keluar (ParkingOut) kendaraan
- **Deteksi Plat Nomor**: Menggunakan algoritma ANPR sendiri
- **WebSocket Server**: Memungkinkan komunikasi realtime antar aplikasi
- **Mode Offline**: Menyimpan data lokal saat koneksi database terputus
- **Monitoring Web**: Dashboard web monitoring untuk status sistem

### IUTVehicleManager
- **Manajemen Kendaraan Terintegrasi**: Mengelola semua aspek kendaraan (tidak hanya parkir)
- **Mode Operasi Configurable**: Dapat dikonfigurasi sebagai mode ENTRY, EXIT, atau ADMIN
- **Integrasi Berbagai Jenis Kendaraan**: Mendukung mobil, motor, bus, dll dengan pelaporan terpisah
- **Manajemen Member/VIP**: Pengelolaan anggota dan kendaraan VIP
- **Dashboard Analitik**: Analisis traffic kendaraan lebih mendalam
- **Sistem Tiket Digital**: Mendukung e-ticket dan pembayaran digital

## Perbedaan Arsitektur

### ParkingIN/ParkingOut
```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  ParkingIN  │◄───►│ MySQL Server │◄───►│   ParkingOut    │
└─────┬───────┘     └──────┬───────┘     └────────┬────────┘
      │                    │                      │
      │                    ▼                      │
      │            ┌──────────────┐               │
      └───────────►│ WebSocket    │◄──────────────┘
                   │    Server    │
                   └──────────────┘
```

- Aplikasi terpisah untuk fungsi masuk dan keluar
- Komunikasi melalui database dan WebSocket
- Fokus pada performa dan ketahanan

### IUTVehicleManager
```
┌───────────────────────────────────────────────┐
│              IUTVehicleManager                │
├─────────────┬─────────────────┬──────────────┤
│ ENTRY Mode  │   EXIT Mode     │  ADMIN Mode  │
└──────┬──────┴────────┬────────┴───────┬──────┘
       │               │                │
       ▼               ▼                ▼
┌──────────────────────────────────────────────┐
│           SQL Server Database                │
└──────────────────────────────────────────────┘
```

- Aplikasi tunggal dengan mode operasi berbeda
- Seluruh logika dalam satu codebase
- Fokus pada integrasi dan kemudahan pengelolaan

## Perbedaan Database

### ParkingIN/ParkingOut
- **Engine**: MySQL
- **Struktur**: Tabel-tabel sederhana dengan relasi minimal
- **Table Utama**: 
  - `t_parkir`: Data masuk dan keluar kendaraan
  - `settings`: Pengaturan aplikasi
  - `tarif`: Konfigurasi tarif parkir

### IUTVehicleManager
- **Engine**: Microsoft SQL Server
- **Struktur**: Skema database yang lebih kompleks
- **Table Utama**:
  - `Vehicles`: Detail kendaraan
  - `ParkingTransactions`: Transaksi parkir
  - `Members`: Informasi anggota
  - `ParkingLots`: Data lokasi parkir
  - `Rates`: Struktur tarif yang lebih kompleks

## Perbedaan UI dan UX

### ParkingIN/ParkingOut
- Interface sederhana dan fokus pada satu tugas (masuk atau keluar)
- Lebih sedikit opsi di layar utama
- Dirancang untuk petugas pos dengan input minimal
- Tampilan klasik Windows Forms

### IUTVehicleManager
- Interface yang lebih kaya dengan fitur
- Design modern dengan animasi dan efek visual
- Dashboard dengan statistik dan grafik
- Mendukung tema gelap/terang
- Tab-based interface untuk akses cepat ke berbagai fitur

## Perbedaan Implementasi Hardware

### ParkingIN/ParkingOut
- Koneksi hardware terpisah per aplikasi
- Implementasi driver per aplikasi
- Konfigurasi terpisah untuk kamera, printer, loop detector
- Watchdog terpisah untuk monitoring koneksi
- Mendukung mikrokontroler ATMEL dengan koneksi RS232/DB9

### IUTVehicleManager
- Layanan hardware terpusat (GateControlService, TicketPrintingService)
- Abstraksi hardware melalui interface
- Mendukung lebih banyak model hardware
- Satu konfigurasi untuk semua perangkat
- Mendukung komunikasi TCP/IP untuk kontrol hardware jarak jauh

## Rekomendasi Penggunaan

### Gunakan ParkingIN/ParkingOut untuk:
- Sistem parkir tradisional dengan pos masuk dan keluar terpisah
- Instalasi dengan hardware yang berbeda-beda di setiap pos
- Kebutuhan offline mode yang kuat dengan sinkronisasi
- Server dengan spesifikasi lebih rendah (MySQL lebih ringan)
- Lingkungan dengan koneksi jaringan tidak stabil/terbatas

### Gunakan IUTVehicleManager untuk:
- Manajemen kendaraan komprehensif (bukan hanya parkir)
- Integrasi dengan sistem manajemen gedung/kampus
- Kebutuhan analitik dan pelaporan yang lebih kompleks
- Dukungan untuk keanggotaan dan langganan
- Integrasi dengan sistem pembayaran digital
- Lingkungan dengan infrastruktur Microsoft yang sudah ada

### Catatan Penting
Untuk instalasi komputer pada pos masuk (GET-IN), berdasarkan kesepakatan sebelumnya, sistem yang digunakan adalah **IUTVehicleManager** yang dikonfigurasi dalam mode ENTRY. Ini merupakan pilihan yang disepakati karena memberikan integrasi lebih baik dengan infrastruktur yang ada dan fitur-fitur tambahan yang dibutuhkan.

Penting untuk tidak mencampuradukkan kedua sistem dalam satu lokasi parkir, karena akan menyebabkan inkonsistensi data dan masalah komunikasi antar komponen. 