# Panduan Instalasi Cursor.sh di Pop!_OS

## Persiapan Sistem

1. Update sistem:
```bash
sudo apt update
sudo apt upgrade -y
```

2. Install dependensi yang diperlukan:
```bash
sudo apt install -y wget curl git
```

## Instalasi Cursor.sh

### Metode 1: Menggunakan AppImage (Direkomendasikan)

1. Download AppImage dari website resmi Cursor:
```bash
wget https://download.cursor.sh/linux/appImage/x64/Cursor-latest.AppImage
```

2. Buat file executable:
```bash
chmod +x Cursor-latest.AppImage
```

3. Pindahkan ke folder aplikasi:
```bash
sudo mkdir -p /opt/cursor
sudo mv Cursor-latest.AppImage /opt/cursor/
```

4. Buat desktop entry untuk integrasi dengan sistem:
```bash
cat << EOF | sudo tee /usr/share/applications/cursor.desktop
[Desktop Entry]
Name=Cursor
Comment=AI-first code editor
Exec=/opt/cursor/Cursor-latest.AppImage
Icon=cursor
Terminal=false
Type=Application
Categories=Development;IDE;
EOF
```

### Metode 2: Menggunakan Debian Package

1. Download paket .deb terbaru:
```bash
wget https://download.cursor.sh/linux/debian/pool/main/c/cursor-editor/cursor-editor_*_amd64.deb
```

2. Install paket:
```bash
sudo apt install ./cursor-editor_*_amd64.deb
```

## Konfigurasi Post-Instalasi

1. Tambahkan Cursor ke grup pengguna:
```bash
sudo usermod -aG $(whoami) cursor
```

2. Set Cursor sebagai default editor (opsional):
```bash
sudo update-alternatives --install /usr/bin/editor editor /opt/cursor/Cursor-latest.AppImage 100
```

## Troubleshooting

### Jika AppImage tidak bisa dijalankan:

1. Install dependensi tambahan:
```bash
sudo apt install -y libfuse2
```

2. Jika masih ada masalah, coba install FUSE:
```bash
sudo apt install -y fuse
```

### Jika ada masalah dengan GPU:

1. Install driver NVIDIA (jika menggunakan GPU NVIDIA):
```bash
sudo apt install -y system76-driver-nvidia
```

2. Atau gunakan driver open source:
```bash
sudo apt install -y mesa-utils
```

## Penggunaan

1. Jalankan Cursor dari menu aplikasi atau terminal:
```bash
/opt/cursor/Cursor-latest.AppImage
```

2. Atau jika menggunakan paket .deb:
```bash
cursor
```

## Uninstall

### Untuk AppImage:
```bash
sudo rm -rf /opt/cursor
sudo rm /usr/share/applications/cursor.desktop
```

### Untuk paket .deb:
```bash
sudo apt remove cursor-editor
```

## Tips Tambahan

1. Untuk performa lebih baik, aktifkan GPU acceleration:
   - Buka Cursor
   - Pergi ke Settings > System
   - Aktifkan "Hardware Acceleration"

2. Integrasi dengan Git:
```bash
git config --global core.editor "cursor --wait"
```

3. Backup konfigurasi:
```bash
cp -r ~/.config/Cursor ~/cursor-config-backup
```

## Catatan Penting

- Pastikan sistem memenuhi persyaratan minimal:
  - RAM: 8GB (direkomendasikan 16GB)
  - Storage: 2GB free space
  - Processor: 4 core CPU
  - GPU: Opsional tapi direkomendasikan
  - Internet: Koneksi stabil untuk fitur AI

- Jika menggunakan VPN, pastikan dapat mengakses:
  - api.cursor.sh
  - download.cursor.sh
  - auth.cursor.sh 