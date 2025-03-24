#!/bin/bash
# monitoring.sh - Script untuk monitoring sistem ParkIRC

# Set warna teks
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}===== MONITORING SISTEM PARKIRC =====${NC}"
echo "Waktu: $(date)"
echo ""

# Check sistem operasi
echo -e "${GREEN}=== Informasi Sistem ===${NC}"
echo -e "Hostname: $(hostname)"
echo -e "Kernel: $(uname -r)"
echo -e "Uptime: $(uptime -p)"
echo ""

# Check PostgreSQL
echo -e "${GREEN}=== Status Database ===${NC}"
if systemctl is-active postgresql >/dev/null; then
    echo -e "PostgreSQL: ${GREEN}AKTIF${NC}"
    # Tambahan info PostgreSQL jika tersedia
    if command -v psql >/dev/null; then
        echo "Database size: $(sudo -u postgres psql -c "SELECT pg_size_pretty(pg_database_size('parkir_db'));" -t 2>/dev/null)"
        echo "Koneksi aktif: $(sudo -u postgres psql -c "SELECT count(*) FROM pg_stat_activity;" -t 2>/dev/null)"
    fi
else
    echo -e "PostgreSQL: ${RED}TIDAK AKTIF${NC}"
fi
echo ""

# Check CUPS
echo -e "${GREEN}=== Status Printer ===${NC}"
if systemctl is-active cups >/dev/null; then
    echo -e "CUPS: ${GREEN}AKTIF${NC}"
    echo "Printer terdaftar:"
    lpstat -p
else
    echo -e "CUPS: ${RED}TIDAK AKTIF${NC}"
fi
echo ""

# Check Application
echo -e "${GREEN}=== Status Aplikasi ===${NC}"
if systemctl is-active parkirc >/dev/null; then
    echo -e "ParkIRC: ${GREEN}AKTIF${NC}"
    echo "Port yang digunakan:"
    netstat -tulpn | grep dotnet
else
    echo -e "ParkIRC: ${RED}TIDAK AKTIF${NC}"
fi
echo ""

# Check Memory
echo -e "${GREEN}=== Penggunaan Memory ===${NC}"
free -h
echo ""

# Check Disk
echo -e "${GREEN}=== Penggunaan Disk ===${NC}"
df -h /opt/parkirc
echo ""

# Check Logs
echo -e "${GREEN}=== Log Terbaru ===${NC}"
if [ -d "/opt/parkirc/logs" ]; then
    tail -n 20 /opt/parkirc/logs/parkirc-$(date +%Y%m%d).log
else
    echo -e "${YELLOW}Directory log tidak ditemukan${NC}"
fi
echo ""

# Check Network
echo -e "${GREEN}=== Status Jaringan ===${NC}"
ip addr | grep "inet " | grep -v "127.0.0.1"
echo ""

echo -e "${GREEN}===== MONITORING SELESAI =====${NC}" 