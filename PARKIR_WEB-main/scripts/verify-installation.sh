#!/bin/bash

# Server checks
echo "Checking Server..."
systemctl status postgresql
systemctl status nginx
systemctl status parkirc

# Check firewall
ufw status

# Check SSL
certbot certificates

# Entry Gate checks
echo "Checking Entry Gate..."
nc -zv server-ip 5432
ls /dev/video0
ls /dev/ttyUSB0
lpstat -p

# Exit Gate checks
echo "Checking Exit Gate..."
nc -zv server-ip 5432
lpstat -p
lsusb | grep "Barcode Scanner" 