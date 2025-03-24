# Panduan Administrator Server

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Arsitektur Sistem](#arsitektur-sistem)
3. [Konfigurasi Database](#konfigurasi-database)
4. [WebSocket Server](#websocket-server)
5. [Manajemen Data Offline](#manajemen-data-offline)
6. [Pemantauan Sistem](#pemantauan-sistem)
7. [Troubleshooting](#troubleshooting)

## Pendahuluan

Panduan ini ditujukan untuk administrator sistem yang bertanggung jawab mengelola infrastruktur server untuk Sistem Parkir Modern. Dokumen ini akan membahas aspek teknis dari sistem, termasuk konfigurasi database, WebSocket server, dan penanganan data offline.

## Arsitektur Sistem

Sistem Parkir Modern terdiri dari beberapa komponen utama:

1. **Aplikasi Klien**:
   - ParkingIN (aplikasi entri kendaraan)
   - ParkingOut (aplikasi keluar kendaraan)
   - SimpleParkingAdmin (panel administrasi)

2. **Server**:
   - Database MySQL
   - WebSocket server
   - File server untuk penyimpanan gambar dan data offline

3. **Konektivitas**:
   - Koneksi TCP/IP antara aplikasi klien dan server
   - WebSocket untuk komunikasi realtime
   - Mekanisme sinkronisasi untuk data offline

![Diagram Arsitektur](../Images/architecture_diagram.png)

## Konfigurasi Database

### Pengaturan Database MySQL

1. **Kredensial Database**:
   - Kredensial default tersimpan di `config/database.ini`
   - Format file konfigurasi:
   ```ini
   [Database]
   Server=localhost
   Port=3306
   Username=parking_user
   Password=secure_password
   Database=parking_system
   ```

2. **Optimasi Performa**:
   - Tambahkan indeks yang sesuai untuk meningkatkan performa query
   - Parameter MySQL yang direkomendasikan:
   ```
   innodb_buffer_pool_size=1G
   max_connections=200
   connect_timeout=10
   wait_timeout=600
   ```

3. **Backup Database**:
   - Jadwalkan backup otomatis setiap hari
   - Simpan backup minimal 30 hari
   - Contoh script backup:
   ```bash
   #!/bin/bash
   TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
   mysqldump -u backup_user -p'backup_password' parking_system > /backup/parking_system_$TIMESTAMP.sql
   ```

## WebSocket Server

WebSocket server berfungsi sebagai jembatan komunikasi realtime antara aplikasi klien.

### Konfigurasi WebSocket

1. **Port dan Endpoint**:
   - Port default: 8181
   - Endpoint: ws://server_ip:8181/parking

2. **File Konfigurasi WebSocket**:
   - Lokasi: `config/websocket.ini`
   - Parameter konfigurasi:
   ```ini
   [WebSocket]
   Port=8181
   MaxConnections=100
   PingInterval=30
   IdleTimeout=300
   ```

3. **Memulai WebSocket Server**:
   - Otomatis dimulai oleh aplikasi server
   - Untuk memulai manual:
   ```
   ParkingServer.exe --websocket-only
   ```

4. **Protokol Komunikasi**:
   - Format pesan: JSON
   - Event types:
     - `parking_entry`: Kendaraan masuk
     - `parking_exit`: Kendaraan keluar
     - `connection_status`: Status koneksi
     - `sync_request`: Permintaan sinkronisasi

## Manajemen Data Offline

Sistem dirancang untuk menangani operasi offline dan menyinkronkan data saat koneksi tersedia kembali.

### Struktur Penyimpanan Data Offline

1. **Lokasi Data Offline**:
   - Root folder: `offline_data/`
   - Subfolder per aplikasi: `ParkingIN/`, `ParkingOut/`

2. **Format File**:
   - JSON untuk data transaksi
   - Contoh nama file: `parking_entry_20231225_153045_A123BC.json`

3. **Schema Data**:
   ```json
   {
     "transaction_id": "T123456",
     "vehicle_number": "A123BC",
     "entry_time": "2023-12-25T15:30:45",
     "vehicle_type": "CAR",
     "image_path": "Images/Masuk/A123BC_20231225_153045.jpg",
     "sync_status": "PENDING"
   }
   ```

### Proses Sinkronisasi

1. **Algoritma Sinkronisasi**:
   - Prioritas FIFO (First In, First Out)
   - Verifikasi data sebelum insert/update ke database
   - Penanganan konfliks dengan strategi "last-write-wins"

2. **Penanganan Kegagalan**:
   - Retry otomatis dengan backoff eksponensial
   - Maksimal 3 kali percobaan
   - Log detail untuk kegagalan

3. **Pemeliharaan Data**:
   - Data offline yang sudah disinkronkan dipindahkan ke `offline_data/archived/`
   - Data arsip dibersihkan otomatis setelah 30 hari

## Pemantauan Sistem

### Logging

1. **Struktur Log**:
   - Lokasi: `logs/`
   - Format file: `application_YYYYMMDD.log`
   - Level log: DEBUG, INFO, WARNING, ERROR, CRITICAL

2. **Contoh Format Log**:
   ```
   [2023-12-25 15:30:45] [INFO] [ParkingInForm] Database connection established
   [2023-12-25 15:35:12] [WARNING] [Database] Connection attempt failed: Timeout
   [2023-12-25 15:40:30] [INFO] [OfflineSync] Started syncing 5 offline records
   ```

3. **Rotasi Log**:
   - Rotasi harian
   - Kompres file log lebih dari 7 hari
   - Hapus file log lebih dari 90 hari

### Metrik Performa

1. **Metrik yang Dimonitor**:
   - Waktu respons database
   - Jumlah koneksi WebSocket aktif
   - Penggunaan memori dan CPU
   - Jumlah transaksi offline menunggu sinkronisasi

2. **Dashboard Monitoring**:
   - Tersedia di SimpleParkingAdmin
   - Path: Administration > System Monitoring

## Troubleshooting

### Masalah Koneksi Database

1. **Koneksi Terputus**:
   - Periksa status server MySQL: `systemctl status mysql`
   - Verifikasi konfigurasi jaringan
   - Periksa kredensial di `config/database.ini`

2. **Koneksi Lambat**:
   - Analisis query dengan `EXPLAIN`
   - Periksa indeks dan optimasi tabel
   - Cek resource server (CPU, memory, disk I/O)

### Masalah WebSocket

1. **Klien Tidak Dapat Terhubung**:
   - Verifikasi port 8181 terbuka di firewall
   - Periksa status WebSocket server di log
   - Coba restart WebSocket server

2. **Pesan Tidak Terkirim**:
   - Periksa koneksi klien
   - Verifikasi format pesan JSON
   - Cek log untuk error parsing

### Masalah Sinkronisasi Data

1. **Data Tidak Tersinkronisasi**:
   - Periksa log di `logs/sync_YYYYMMDD.log`
   - Verifikasi file data offline masih ada
   - Cek permissions pada folder dan file

2. **Data Duplikat**:
   - Identifikasi record duplikat di database
   - Periksa log sinkronisasi untuk anomali
   - Hapus duplikat melalui panel admin

3. **Resolusi Manual**:
   - Gunakan tool `SyncTool.exe` untuk sinkronisasi manual:
   ```
   SyncTool.exe --source offline_data/ParkingIN --force-sync
   ```

## Pemeliharaan Rutin

1. **Backup Database**:
   - Daily full backup
   - Verifikasi integritas backup secara berkala

2. **Pembersihan Data**:
   - Arsip data transaksi > 1 tahun
   - Bersihkan log > 90 hari
   - Hapus gambar kendaraan yang sudah keluar > 30 hari

3. **Update Sistem**:
   - Jadwalkan update pada jam non-peak
   - Selalu backup sebelum update
   - Ikuti prosedur update di `docs/update_procedure.md`

Untuk informasi lebih lanjut atau bantuan, hubungi tim pengembang di developer@parkingsystem.com atau buka ticket support di sistem ticketing internal. 