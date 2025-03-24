#!/bin/bash
# backup.sh - Script untuk backup sistem ParkIRC

# Konfigurasi
BACKUP_DIR="/opt/parkirc/backups"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="parkir_db"
APP_DIR="/opt/parkirc"

# Set warna teks
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}===== BACKUP SISTEM PARKIRC =====${NC}"
echo "Waktu: $(date)"
echo "Backup directory: $BACKUP_DIR"
echo ""

# Buat direktori backup jika belum ada
if [ ! -d "$BACKUP_DIR" ]; then
    mkdir -p "$BACKUP_DIR"
    echo -e "${YELLOW}Direktori backup dibuat${NC}"
fi

# Backup database PostgreSQL
echo -e "${GREEN}=== Backup Database ===${NC}"
if command -v pg_dump >/dev/null; then
    pg_dump -Fc $DB_NAME > $BACKUP_DIR/db_$DATE.dump
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Database backup selesai:${NC} $BACKUP_DIR/db_$DATE.dump"
    else
        echo -e "${RED}Database backup gagal!${NC}"
    fi
else
    echo -e "${RED}pg_dump tidak ditemukan. Database tidak di-backup.${NC}"
fi
echo ""

# Backup uploads
echo -e "${GREEN}=== Backup File Upload ===${NC}"
if [ -d "$APP_DIR/wwwroot/uploads" ]; then
    tar -czf $BACKUP_DIR/uploads_$DATE.tar.gz $APP_DIR/wwwroot/uploads
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Upload backup selesai:${NC} $BACKUP_DIR/uploads_$DATE.tar.gz"
    else
        echo -e "${RED}Upload backup gagal!${NC}"
    fi
else
    echo -e "${YELLOW}Directory uploads tidak ditemukan. Tidak ada file yang di-backup.${NC}"
fi
echo ""

# Backup konfigurasi
echo -e "${GREEN}=== Backup Konfigurasi ===${NC}"
if [ -f "$APP_DIR/appsettings.json" ]; then
    cp $APP_DIR/appsettings.json $BACKUP_DIR/appsettings_$DATE.json
    echo -e "${GREEN}Konfigurasi backup selesai:${NC} $BACKUP_DIR/appsettings_$DATE.json"
else
    echo -e "${YELLOW}File konfigurasi tidak ditemukan.${NC}"
fi
echo ""

# Housekeeping - hapus backup lama (lebih dari 7 hari)
echo -e "${GREEN}=== Housekeeping ===${NC}"
echo "Menghapus backup yang lebih lama dari 7 hari..."
find $BACKUP_DIR -name "db_*" -mtime +7 -delete
find $BACKUP_DIR -name "uploads_*" -mtime +7 -delete
find $BACKUP_DIR -name "appsettings_*" -mtime +7 -delete
echo -e "${GREEN}Housekeeping selesai${NC}"
echo ""

# Tampilkan informasi backup
echo -e "${GREEN}=== Ringkasan Backup ===${NC}"
echo "Total size backup:"
du -sh $BACKUP_DIR
echo "Backup files:"
ls -lh $BACKUP_DIR | grep $DATE
echo ""

echo -e "${GREEN}===== BACKUP SELESAI =====${NC}" 