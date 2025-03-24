# Dokumentasi Fitur Database Realtime dan Notifikasi Koneksi

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [WebSocket dan Database Realtime](#websocket-dan-database-realtime)
3. [Indikator Status Koneksi](#indikator-status-koneksi)
4. [Mode Offline](#mode-offline)
5. [Sinkronisasi Data](#sinkronisasi-data)
6. [Pemecahan Masalah](#pemecahan-masalah)

## Pendahuluan

Sistem Parkir Modern dilengkapi dengan fitur database realtime dan sistem notifikasi koneksi yang memungkinkan aplikasi tetap berfungsi meskipun koneksi database terputus. Dokumen ini menjelaskan cara menggunakan fitur-fitur tersebut.

## WebSocket dan Database Realtime

Sistem Parkir Modern menggunakan teknologi WebSocket untuk memungkinkan komunikasi dua arah antara server dan aplikasi klien. Hal ini memungkinkan:

1. **Update Data Realtime**: Perubahan data pada satu aplikasi akan langsung terlihat di aplikasi lain yang terhubung
2. **Notifikasi Instan**: Pemberitahuan langsung saat kendaraan masuk atau keluar
3. **Status Sistem**: Pemantauan kondisi server dan database secara realtime

WebSocket server berjalan di port 8181 dan secara otomatis dimulai ketika Anda menjalankan komponen Server dari menu utama.

## Indikator Status Koneksi

Semua aplikasi klien (ParkingIN, ParkingOut, dan SimpleParkingAdmin) dilengkapi dengan indikator status koneksi database yang terletak di pojok kanan atas window aplikasi.

### Status Indikator:

1. **Memeriksa Koneksi** (Abu-abu): Aplikasi sedang memeriksa koneksi ke database
2. **Database Terhubung** (Hijau): Koneksi ke database aktif dan berfungsi normal
3. **Database Terputus** (Merah): Koneksi ke database terputus, aplikasi berjalan dalam mode offline

Indikator ini akan secara otomatis memeriksa status koneksi setiap 10 detik dan memperbarui tampilan sesuai kondisi terkini.

![Indikator Status Koneksi](../Images/connection_indicator.png)

## Mode Offline

Ketika koneksi database terputus (misalnya karena masalah jaringan atau server database mati), aplikasi akan secara otomatis beralih ke mode offline. Dalam mode ini:

### ParkingIN (Aplikasi Masuk Kendaraan):
- Tetap dapat memproses kendaraan yang masuk
- Data disimpan secara lokal di file JSON
- Data akan disinkronkan ke database saat koneksi tersedia kembali
- Foto kendaraan tetap disimpan di folder lokal

### ParkingOut (Aplikasi Keluar Kendaraan):
- Tidak dapat mencari data kendaraan yang masuk
- Tetap dapat memproses kendaraan keluar jika datanya sudah ditemukan sebelumnya
- Data keluar kendaraan disimpan secara lokal
- Data akan disinkronkan ke database saat koneksi tersedia kembali

### SimpleParkingAdmin (Panel Admin):
- Sebagian besar fitur tidak tersedia dalam mode offline
- Dapat melihat log dan statistik yang sudah dimuat sebelumnya
- Tidak dapat mengubah konfigurasi sistem

## Sinkronisasi Data

Ketika koneksi database tersedia kembali setelah periode offline:

1. Indikator status koneksi akan berubah menjadi hijau
2. Jika terdapat data offline, sistem akan menampilkan dialog konfirmasi untuk menyinkronkan data
3. Anda dapat memilih untuk sinkronisasi sekarang atau nanti

![Dialog Sinkronisasi](../Images/sync_dialog.png)

Proses sinkronisasi:
- Data offline akan dikirim ke database satu per satu
- Dialog progres akan ditampilkan selama proses berlangsung
- Setelah selesai, ringkasan sinkronisasi akan ditampilkan

**Catatan**: Jika proses sinkronisasi gagal, data offline tetap tersimpan dan Anda dapat mencoba sinkronisasi lagi nanti.

## Penyimpanan Data Offline

Data offline disimpan dalam:

1. **Folder offline_data**: Berisi file JSON untuk setiap transaksi
   - Format: `parking_entry_[tanggal]_[waktu]_[id].json` untuk data masuk
   - Format: `parking_exit_[tanggal]_[waktu]_[id].json` untuk data keluar

2. **Folder Images**: Menyimpan foto kendaraan
   - Subfolder `Masuk`: Foto kendaraan masuk
   - Subfolder `Keluar`: Foto kendaraan keluar

Data ini akan dipelihara otomatis oleh aplikasi dan dibersihkan setelah berhasil disinkronkan ke database.

## Pemecahan Masalah

### Koneksi Sering Terputus
1. Periksa koneksi jaringan antara aplikasi klien dan server
2. Pastikan layanan MySQL berjalan di server
3. Periksa konfigurasi kredensial database
4. Periksa log aplikasi di folder `logs`

### Data Tidak Tersinkronisasi
1. Buka aplikasi dan periksa status koneksi
2. Pastikan koneksi database aktif (indikator hijau)
3. Pilih opsi "Sinkronisasi Data" dari menu (jika tersedia)
4. Periksa folder `offline_data` untuk memastikan data tersimpan dengan benar

### Data Duplikat
Jika terjadi data duplikat setelah sinkronisasi:
1. Gunakan panel admin untuk mengidentifikasi dan menghapus data duplikat
2. Periksa log sinkronisasi untuk menemukan penyebab duplikasi

### Error Saat Sinkronisasi
Jika terjadi error saat sinkronisasi:
1. Catat pesan error yang muncul
2. Periksa log aplikasi untuk detail lebih lanjut
3. Selesaikan masalah yang teridentifikasi (misalnya isu koneksi)
4. Coba sinkronisasi ulang

Untuk bantuan lebih lanjut, hubungi administrator sistem atau lihat [Manual Administrator Server](administrator_server_guide.md). 