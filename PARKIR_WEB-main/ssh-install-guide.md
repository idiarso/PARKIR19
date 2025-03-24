# Panduan Instalasi dan Konfigurasi SSH di Pop!_OS

## Instalasi SSH Server

1. Update sistem terlebih dahulu:
```bash
sudo apt update
sudo apt upgrade -y
```

2. Install OpenSSH Server:
```bash
sudo apt install openssh-server -y
```

3. Periksa status SSH:
```bash
sudo systemctl status ssh
```

4. Aktifkan SSH service agar berjalan saat startup:
```bash
sudo systemctl enable ssh
```

## Konfigurasi Firewall

1. Izinkan SSH melalui firewall:
```bash
sudo ufw allow ssh
```

2. Aktifkan firewall:
```bash
sudo ufw enable
```

3. Periksa status firewall:
```bash
sudo ufw status
```

## Konfigurasi SSH Server

1. Backup konfigurasi default:
```bash
sudo cp /etc/ssh/sshd_config /etc/ssh/sshd_config.bak
```

2. Edit konfigurasi SSH (gunakan editor pilihan Anda):
```bash
sudo nano /etc/ssh/sshd_config
```

3. Rekomendasi pengaturan keamanan (tambahkan/edit di sshd_config):
```bash
# Nonaktifkan login root
PermitRootLogin no

# Gunakan protokol SSH versi 2
Protocol 2

# Batasi jumlah percobaan autentikasi
MaxAuthTries 3

# Atur timeout untuk idle sessions
ClientAliveInterval 300
ClientAliveCountMax 2

# Batasi user yang bisa menggunakan SSH
AllowUsers yourusername

# Nonaktifkan autentikasi password (jika menggunakan key-based auth)
PasswordAuthentication no
```

4. Restart SSH service setelah mengubah konfigurasi:
```bash
sudo systemctl restart ssh
```

## Setup Key-Based Authentication (Direkomendasikan)

1. Generate SSH key di komputer client:
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

2. Copy public key ke server:
```bash
ssh-copy-id username@server_ip
```

Atau manual:
```bash
# Di client
cat ~/.ssh/id_ed25519.pub

# Di server
mkdir -p ~/.ssh
chmod 700 ~/.ssh
echo "public_key_string" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

## Penggunaan

1. Koneksi ke server SSH:
```bash
ssh username@server_ip
```

2. Transfer file menggunakan SCP:
```bash
# Upload file
scp local_file username@server_ip:/remote/path

# Download file
scp username@server_ip:/remote/file local_path
```

## Troubleshooting

1. Jika tidak bisa connect:
```bash
# Periksa SSH service
sudo systemctl status ssh

# Periksa port SSH (default 22)
sudo netstat -tulpn | grep ssh

# Periksa logs
sudo tail -f /var/log/auth.log
```

2. Jika permission denied:
```bash
# Periksa permission file .ssh
ls -la ~/.ssh

# Perbaiki permission jika perlu
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

## Tips Keamanan

1. Ganti port default SSH (22):
```bash
# Edit /etc/ssh/sshd_config
Port 2222  # Ganti dengan port pilihan Anda
```

2. Gunakan Fail2ban untuk proteksi brute force:
```bash
sudo apt install fail2ban
sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
sudo nano /etc/fail2ban/jail.local
```

3. Setup Two-Factor Authentication (2FA):
```bash
sudo apt install libpam-google-authenticator
google-authenticator
```

## Backup dan Restore

1. Backup konfigurasi SSH:
```bash
sudo tar -czf ssh_backup.tar.gz /etc/ssh/
```

2. Backup SSH keys:
```bash
tar -czf ssh_keys_backup.tar.gz ~/.ssh/
```

## Catatan Penting

- Selalu backup konfigurasi sebelum melakukan perubahan
- Jangan share private key dengan siapapun
- Gunakan password yang kuat
- Update sistem secara regular
- Monitor log SSH untuk aktivitas mencurigakan
- Pertimbangkan menggunakan fail2ban untuk keamanan tambahan 