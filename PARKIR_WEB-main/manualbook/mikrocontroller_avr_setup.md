# Pengaturan Mikrokontroler ATMEL (AVR) dengan Koneksi DB9

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Komponen yang Dibutuhkan](#komponen-yang-dibutuhkan)
3. [Koneksi Hardware](#koneksi-hardware)
4. [Pemrograman Mikrokontroler](#pemrograman-mikrokontroler)
5. [Konfigurasi Koneksi RS232](#konfigurasi-koneksi-rs232)
6. [Deteksi Sinyal dan Monitoring](#deteksi-sinyal-dan-monitoring)
7. [Troubleshooting](#troubleshooting)

## Pendahuluan

Sistem Parkir Modern mendukung integrasi dengan mikrokontroler ATMEL AVR (seperti ATmega16/32) yang terhubung melalui port serial RS232 dengan konektor DB9. Mikrokontroler ini berfungsi sebagai pengendali perangkat keras (gate controller, loop detector) dan menjembatani komunikasi antara hardware parkir dengan aplikasi ParkingIN dan ParkingOut.

## Komponen yang Dibutuhkan

1. **Mikrokontroler**:
   - DT-AVR Low Cost Micro System berbasis ATmega16 atau ATmega32
   - Alternatif: Board Arduino dengan shield RS232

2. **Konverter Level**:
   - Modul RS232 ke TTL (MAX232 atau setara)
   - Kabel konverter USB ke RS232 (jika komputer tidak memiliki port DB9)

3. **Kabel dan Konektor**:
   - Kabel DB9 Male to Female
   - Konektor DB9 Male/Female
   - Kabel jumper

4. **Software**:
   - CodeVision AVR atau BASCOM AVR (untuk pemrograman)
   - AVR Studio
   - AVR Dude GUI (untuk upload program)
   - Progisp (alternatif untuk upload program)

5. **Power Supply**:
   - Adaptor 12V DC (minimal 1A)
   - Regulator tegangan 5V

## Koneksi Hardware

### Diagram Koneksi
![Diagram Koneksi](../Images/avr_connection_diagram.png)

### Koneksi DB9 ke Mikrokontroler

| Pin DB9 | Fungsi RS232 | Pin MAX232 | Pin ATmega16/32 |
|---------|--------------|------------|-----------------|
| 2       | RXD          | T1OUT      | PD0 (RXD)       |
| 3       | TXD          | R1IN       | PD1 (TXD)       |
| 5       | GND          | GND        | GND             |
| 7       | RTS          | T2OUT      | PD2 (INT0)      |
| 8       | CTS          | R2IN       | PD3 (INT1)      |

### Koneksi Perangkat Parkir

1. **Loop Detector**:
   - Output Loop Detector → Pin PC0 (ADC0) ATmega16/32
   - GND Loop Detector → GND ATmega16/32

2. **Gate Controller**:
   - Relay Buka Gate → Pin PB0 ATmega16/32
   - Relay Tutup Gate → Pin PB1 ATmega16/32
   - GND → GND ATmega16/32

3. **Sensor Tambahan** (opsional):
   - Sensor Infrared → Pin PC1 (ADC1) ATmega16/32
   - Sensor Ultrasonik → Pin PC2 (ADC2) ATmega16/32

### Koneksi Power Supply
- VCC (5V) → VCC ATmega16/32
- GND → GND ATmega16/32
- 12V → Input MAX232 (jika diperlukan)

## Pemrograman Mikrokontroler

### Konfigurasi Awal CodeVision AVR

1. Buat project baru di CodeVision AVR
2. Pilih chip ATmega16/32
3. Konfigurasi clock: 16 MHz (eksternal) atau 8 MHz (internal)
4. Konfigurasi port USART:
   - Baud rate: 9600
   - Data bits: 8
   - Parity: None
   - Stop bits: 1
   - Mode: Asynchronous

### Kode Dasar untuk ATmega16/32

```c
#include <mega16.h>
#include <delay.h>
#include <stdio.h>
#include <string.h>

// Definisi pin
#define LOOP_SENSOR_PIN PINC.0
#define GATE_OPEN_PIN PORTB.0
#define GATE_CLOSE_PIN PORTB.1
#define STATUS_LED_PIN PORTB.2

// Buffer untuk komunikasi serial
char rx_buffer[32];
unsigned char rx_index = 0;
char command[32];
bool command_ready = false;

// Status sistem
bool vehicle_detected = false;
bool gate_open = false;
bool is_opening = false;
bool is_closing = false;
unsigned int timeout_counter = 0;

// Interrupt receiver USART
interrupt [USART_RXC] void usart_rx_isr(void) {
    char received;
    received = UDR; // Baca data dari USART
    
    // Jika menerima karakter baris baru, tandai command ready
    if (received == '\r' || received == '\n') {
        if (rx_index > 0) {
            rx_buffer[rx_index] = 0; // Null terminator
            strcpy(command, rx_buffer);
            command_ready = true;
            rx_index = 0;
        }
    } else {
        // Tambahkan karakter ke buffer
        if (rx_index < sizeof(rx_buffer) - 1) {
            rx_buffer[rx_index++] = received;
        }
    }
}

// Interrupt timer untuk timeout dan pengecekan status
interrupt [TIM0_OVF] void timer0_ovf_isr(void) {
    TCNT0 = 0x06; // Reload timer (250ms dengan prescaler 1024)
    
    // Check timeout untuk gate operations
    if (is_opening || is_closing) {
        timeout_counter++;
        if (timeout_counter >= 20) { // 5 detik (20 * 250ms)
            is_opening = false;
            is_closing = false;
            timeout_counter = 0;
            
            // Update status gate
            if (is_opening) gate_open = true;
            if (is_closing) gate_open = false;
            
            if (gate_open) {
                printf("GATE_STATUS:OPEN\r\n");
            } else {
                printf("GATE_STATUS:CLOSED\r\n");
            }
        }
    }
}

// Inisialisasi USART
void init_usart(void) {
    UCSRA = 0x00;
    UCSRB = 0x98; // Enable receiver, transmitter dan interrupt receiver
    UCSRC = 0x86; // Mode asynchronous, 8 data bits, 1 stop bit, no parity
    UBRRH = 0x00; // Baud rate 9600 dengan crystal 16MHz
    UBRRL = 0x67;
}

// Inisialisasi Timer0
void init_timer0(void) {
    TCCR0 = 0x07; // Prescaler 1024
    TCNT0 = 0x06; // Reload value untuk 250ms
    TIMSK = 0x01; // Enable timer0 overflow interrupt
}

// Inisialisasi system
void init_system(void) {
    // Konfigurasi port
    DDRB = 0x07; // PB0, PB1, PB2 sebagai output
    PORTB = 0x00; // Semua output LOW
    
    DDRC = 0x00; // PORTC sebagai input
    PORTC = 0x01; // Pull-up untuk PC0 (loop sensor)
    
    init_usart();
    init_timer0();
    
    // Aktifkan global interrupt
    #asm("sei")
    
    // Kirim startup message
    printf("PARKING_CONTROLLER_READY\r\n");
    
    // Blink LED sebagai indikasi startup
    for (int i = 0; i < 5; i++) {
        STATUS_LED_PIN = 1;
        delay_ms(100);
        STATUS_LED_PIN = 0;
        delay_ms(100);
    }
}

// Buka gate
void open_gate(void) {
    if (!is_opening) {
        // Hentikan operasi tutup jika sedang berjalan
        if (is_closing) {
            GATE_CLOSE_PIN = 0;
            is_closing = false;
            delay_ms(100);
        }
        
        is_opening = true;
        timeout_counter = 0;
        
        GATE_OPEN_PIN = 1;
        printf("GATE_STATUS:OPENING\r\n");
        delay_ms(1000);
        GATE_OPEN_PIN = 0;
    }
}

// Tutup gate
void close_gate(void) {
    if (!is_closing && !vehicle_detected) {
        // Hentikan operasi buka jika sedang berjalan
        if (is_opening) {
            GATE_OPEN_PIN = 0;
            is_opening = false;
            delay_ms(100);
        }
        
        is_closing = true;
        timeout_counter = 0;
        
        GATE_CLOSE_PIN = 1;
        printf("GATE_STATUS:CLOSING\r\n");
        delay_ms(1000);
        GATE_CLOSE_PIN = 0;
    } else if (vehicle_detected) {
        printf("GATE_ERROR:VEHICLE_DETECTED\r\n");
    }
}

// Periksa status komando
void check_commands(void) {
    if (command_ready) {
        command_ready = false;
        
        if (strcmp(command, "OPEN_GATE") == 0) {
            open_gate();
        } else if (strcmp(command, "CLOSE_GATE") == 0) {
            close_gate();
        } else if (strcmp(command, "GET_STATUS") == 0) {
            // Kirim status lengkap
            printf("STATUS:VEHICLE=%s,GATE=%s\r\n", 
                vehicle_detected ? "DETECTED" : "NONE",
                gate_open ? "OPEN" : "CLOSED");
        } else if (strcmp(command, "PING") == 0) {
            printf("PONG\r\n");
        }
    }
}

// Periksa status sensor loop
void check_loop_sensor(void) {
    static bool last_sensor_state = false;
    bool current_state = !LOOP_SENSOR_PIN; // Active low
    
    if (current_state != last_sensor_state) {
        last_sensor_state = current_state;
        vehicle_detected = current_state;
        
        STATUS_LED_PIN = vehicle_detected;
        
        if (vehicle_detected) {
            printf("VEHICLE_DETECTED\r\n");
        } else {
            printf("NO_VEHICLE\r\n");
        }
        
        delay_ms(50); // Debounce delay
    }
}

void main(void) {
    init_system();
    
    while (1) {
        check_commands();
        check_loop_sensor();
        delay_ms(10); // Delay untuk stabilitas
    }
}
```

### Upload Program ke Mikrokontroler dengan AVR Dude GUI

1. Compile program di CodeVision AVR untuk mendapatkan file HEX
2. Jalankan AVR Dude GUI
3. Konfigurasi parameter:
   - Programmer: STK500v2
   - Port: COM port yang sesuai (misal COM1)
   - Chip: ATmega16 atau ATmega32
   - Program File: Pilih file HEX hasil compile
4. Pada tab "Fuses", atur sesuai kebutuhan:
   - Low Fuse: 0xEF (untuk crystal eksternal)
   - High Fuse: 0xD9 (untuk protect bootloader)
5. Klik "Execute" untuk memulai proses upload
6. Verifikasi proses upload berhasil dari log yang ditampilkan

## Konfigurasi Koneksi RS232

### Pengaturan Port RS232 di Komputer
1. Buka Device Manager di Windows
2. Cari "Ports (COM & LPT)"
3. Catat nomor COM port untuk konverter USB-RS232 atau port serial COM
4. Pastikan konfigurasi port:
   - Baud rate: 9600
   - Data bits: 8
   - Stop bits: 1
   - Parity: None
   - Flow control: None

### Konfigurasi di Aplikasi ParkingIN/ParkingOut
1. Buka aplikasi ParkingIN atau ParkingOut
2. Pilih menu **Pengaturan > Hardware > RS232**
3. Masukkan parameter:
   - Port: COM port yang tercatat di Device Manager
   - Baud Rate: 9600
   - Data Bits: 8
   - Parity: None
   - Stop Bits: 1
   - Handshaking: None
4. Klik "Test Connection" untuk verifikasi koneksi
5. Simpan pengaturan

## Deteksi Sinyal dan Monitoring

### Protokol Komunikasi
Sistem menggunakan protokol ASCII text sederhana melalui RS232:

**Pesan dari Mikrokontroler ke Aplikasi:**
- `PARKING_CONTROLLER_READY`: Mikrokontroler telah siap
- `VEHICLE_DETECTED`: Kendaraan terdeteksi pada loop
- `NO_VEHICLE`: Tidak ada kendaraan pada loop
- `GATE_STATUS:OPENING`: Gate sedang dalam proses membuka
- `GATE_STATUS:OPEN`: Gate telah terbuka
- `GATE_STATUS:CLOSING`: Gate sedang dalam proses menutup
- `GATE_STATUS:CLOSED`: Gate telah tertutup
- `GATE_ERROR:XXX`: Error pada operasi gate dengan kode XXX
- `PONG`: Respon terhadap ping dari aplikasi

**Perintah dari Aplikasi ke Mikrokontroler:**
- `OPEN_GATE`: Perintah membuka gate
- `CLOSE_GATE`: Perintah menutup gate
- `GET_STATUS`: Permintaan status sistem
- `PING`: Pemeriksaan koneksi

### Algoritma Deteksi Koneksi Terputus

Sistem mengimplementasikan mekanisme watchdog untuk mendeteksi jika koneksi terputus:

1. **Ping-Pong Mechanism**:
   - Aplikasi secara periodik mengirim `PING` setiap 2 detik
   - Mikrokontroler harus merespon dengan `PONG`
   - Jika tidak ada respon setelah 3 kali ping berturut-turut, koneksi dianggap terputus

2. **Timeout Detection**:
   - Jika tidak ada pesan diterima dari mikrokontroler selama 5 detik, koneksi dianggap terputus

3. **Visual Indicator**:
   - LED pada mikrokontroler berkedip saat koneksi aktif
   - LED mati saat koneksi terputus
   - Aplikasi menampilkan indikator status di pojok kanan bawah

### Monitoring dengan Serial Monitor

Untuk debugging dan monitoring komunikasi:

1. Gunakan Serial Monitor di aplikasi ParkingIN/ParkingOut:
   - Buka menu **Tools > Serial Monitor**
   - Pilih COM port yang digunakan
   - Set Baud Rate: 9600
   - Klik "Start Monitoring"

2. Atau gunakan aplikasi terminal serial seperti PuTTY:
   - Pilih Connection type: Serial
   - COM port yang sesuai
   - Speed: 9600
   - Klik "Open"

## Troubleshooting

### Masalah Koneksi

| Masalah | Penyebab | Solusi |
|---------|----------|--------|
| Komunikasi gagal | Konfigurasi serial tidak sesuai | Periksa Baud rate, data bits, parity, dan stop bits |
| | Kabel rusak/longgar | Periksa koneksi kabel DB9 |
| | MAX232 tidak berfungsi | Ganti modul MAX232 |
| Device not found | COM port salah | Verifikasi COM port di Device Manager |
| | Driver tidak terinstal | Install driver USB-to-Serial |
| Pesan error | Mikrokontroler reset | Periksa power supply |
| | Noise pada jalur komunikasi | Gunakan kabel shielded |

### Masalah Hardware

| Masalah | Solusi |
|---------|--------|
| Gate tidak merespon | - Periksa koneksi relay ke mikrokontroler<br>- Verifikasi tegangan operasi relay<br>- Periksa kabel dari mikrokontroler ke gate controller |
| Loop detector tidak mendeteksi | - Verifikasi sinyal dari loop detector<br>- Sesuaikan sensitivitas loop detector<br>- Periksa kabel dari loop detector ke mikrokontroler |
| ATmega tidak berfungsi | - Periksa power supply mikrokontroler<br>- Reset mikrokontroler<br>- Periksa fuse bits<br>- Program ulang mikrokontroler |

### Mengatasi Interferensi

1. Gunakan kabel berpelindung (shielded cable) untuk komunikasi RS232
2. Pasang ferrite core pada kabel RS232 dan power supply
3. Pisahkan jalur kabel power dan sinyal
4. Pastikan grounding yang baik untuk semua perangkat
5. Jauhi kabel dari sumber interferensi elektromagnetik seperti motor listrik atau transformator

### Tips Reset dan Recovery

1. Jika mikrokontroler tidak merespon:
   - Tekan tombol reset pada board
   - Tunggu inisialisasi ulang (ditandai dengan LED berkedip)
   - Periksa komunikasi dengan Serial Monitor

2. Jika terjadi kerusakan firmware:
   - Gunakan AVR Dude GUI untuk memprogram ulang
   - Atau gunakan Progisp untuk flashing firmware

3. Backup firmware:
   - Simpan file HEX yang sudah berfungsi dengan baik
   - Dokumentasikan setting fuse bits

4. Jika komunikasi RS232 tetap bermasalah:
   - Coba gunakan baudrate yang lebih rendah (4800)
   - Verifikasi level tegangan RS232 (+/-12V)
   - Periksa tegangan output MAX232 