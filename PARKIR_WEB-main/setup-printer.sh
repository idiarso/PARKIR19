#!/bin/bash

echo "Installing printer on Pop!_OS..."

# Install CUPS dan dependencies
sudo apt update
sudo apt install -y \
    cups \
    cups-client \
    cups-daemon \
    printer-driver-escpr \
    libcups2 \
    libcupsimage2

# Start dan enable CUPS service
sudo systemctl start cups
sudo systemctl enable cups

# Konfigurasi CUPS untuk akses network
sudo cupsctl --remote-any
sudo systemctl restart cups

# Tambahkan user ke group lpadmin untuk manajemen printer
sudo usermod -a -G lpadmin $USER

# Download driver Epson
TEMP_DIR=$(mktemp -d)
cd $TEMP_DIR

echo "Downloading Epson driver..."
# Untuk TM-T82, kita perlu driver APD (Advanced Printer Driver)
wget https://download.epson-biz.com/modules/pos/index.php?page=single_soft&cid=6566&pcat=3&scat=31 -O epson-apt.tar.gz

if [ $? -ne 0 ]; then
    echo "Error downloading driver. Please check your internet connection."
    exit 1
fi

# Extract dan install driver
echo "Installing Epson driver..."
tar xzf epson-apt.tar.gz
cd epson-apt
sudo ./install.sh

# Tambahkan printer
echo "Adding printer to CUPS..."
sudo lpadmin -p EPSON_TM_T82 -E -v usb://EPSON/TM-T82 -m epson.ppd
sudo lpoptions -d EPSON_TM_T82

# Test printer
echo "Testing printer connection..."
lpstat -p EPSON_TM_T82

# Cleanup
cd
rm -rf $TEMP_DIR

echo "Installation complete!"
echo "Please check printer status with: lpstat -p EPSON_TM_T82"
echo "You may need to log out and log back in for group changes to take effect."

# Tambahkan informasi troubleshooting
echo "
Troubleshooting:
1. If printer not detected, check USB connection with: lsusb
2. Check CUPS status: systemctl status cups
3. View CUPS error log: tail -f /var/log/cups/error_log
4. Access CUPS web interface: http://localhost:631
" 