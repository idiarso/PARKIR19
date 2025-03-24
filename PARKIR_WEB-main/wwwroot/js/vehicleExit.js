let html5QrcodeScanner = null;
let currentStream = null;
let selectedDeviceId = null;
let isAutoProcessEnabled = true;

// Initialize DataTable for recent exits
const recentExitsTable = $('#recentExitsTable').DataTable({
    order: [[0, 'desc']],
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/id.json'
    }
});

// Auto Process Switch Handler
document.getElementById('autoProcessSwitch').addEventListener('change', function() {
    isAutoProcessEnabled = this.checked;
});

// Initialize camera handling
async function initializeCamera() {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices.filter(device => device.kind === 'videoinput');
        
        if (videoDevices.length === 0) {
            throw new Error('No camera found');
        }

        // Use the first camera by default or the selected one
        const deviceId = selectedDeviceId || videoDevices[0].deviceId;
        const stream = await navigator.mediaDevices.getUserMedia({
            video: {
                deviceId: deviceId,
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: "environment"
            }
        });

        const video = document.getElementById('previewCamera');
        video.srcObject = stream;
        currentStream = stream;
        
        // Start scanning when video is playing
        video.addEventListener('play', () => {
            startScanning();
        });

        await video.play();
        
        // Show scan status
        document.getElementById('scanStatus').style.display = 'block';
        
        return true;
    } catch (error) {
        console.error('Error accessing camera:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal mengakses kamera: ' + error.message
        });
        return false;
    }
}

// Start scanning function
function startScanning() {
    const video = document.getElementById('previewCamera');
    const canvas = document.getElementById('previewCanvas');
    const ctx = canvas.getContext('2d');
    
    // Set canvas size to match video
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    
    // Scanning interval
    const scanInterval = setInterval(() => {
        if (!currentStream) {
            clearInterval(scanInterval);
            return;
        }

        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        
        try {
            // Use ZXing or other barcode library to decode
            const code = jsQR(imageData.data, imageData.width, imageData.height);
            
            if (code) {
                // Play beep sound
                const beep = new Audio('data:audio/wav;base64,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');
                beep.play();

                // Process the code
                handleScannedCode(code.data);
            }
        } catch (error) {
            console.error('Error scanning:', error);
        }
    }, 100); // Scan every 100ms
}

// Handle scanned code
async function handleScannedCode(decodedText) {
    console.log(`Code scanned = ${decodedText}`);
    
    // Parse barcode data (format: vehicleNumber|entryTime)
    const [vehicleNumber] = decodedText.split('|');
    document.getElementById('vehicleNumber').value = vehicleNumber;
    
    // Update scan status
    document.getElementById('scanStatusText').textContent = `Barcode terdeteksi: ${vehicleNumber}`;
    
    if (isAutoProcessEnabled) {
        // Stop scanning temporarily
        stopCamera();
        
        // Show loading
        Swal.fire({
            title: 'Memproses...',
            text: `Nomor Kendaraan: ${vehicleNumber}`,
            icon: 'info',
            allowOutsideClick: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        try {
            // Search vehicle
            const response = await fetch(`/api/Parking/GetVehicleDetails?vehicleNumber=${encodeURIComponent(vehicleNumber)}`);
            const data = await response.json();

            if (data.success) {
                updateParkingDetails(data.vehicle);
                // Process exit
                await processVehicleExit(vehicleNumber);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: data.message || 'Kendaraan tidak ditemukan'
                });
                clearForm();
                // Restart scanning
                initializeCamera();
            }
        } catch (error) {
            console.error('Error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Gagal memproses data kendaraan'
            });
            // Restart scanning
            initializeCamera();
        }
    } else {
        // Just search for vehicle details
        searchVehicle(vehicleNumber);
    }
}

// Start button handler
document.getElementById('startButton').addEventListener('click', async function() {
    const success = await initializeCamera();
    if (success) {
        this.style.display = 'none';
        document.getElementById('stopButton').style.display = 'block';
    }
});

// Stop button handler
document.getElementById('stopButton').addEventListener('click', function() {
    stopCamera();
    this.style.display = 'none';
    document.getElementById('startButton').style.display = 'block';
    document.getElementById('scanStatus').style.display = 'none';
});

// Switch camera handler
document.getElementById('switchCamera').addEventListener('click', async function() {
    const devices = await navigator.mediaDevices.enumerateDevices();
    const videoDevices = devices.filter(device => device.kind === 'videoinput');
    
    if (videoDevices.length <= 1) {
        Swal.fire({
            icon: 'info',
            title: 'Info',
            text: 'Hanya satu kamera yang tersedia'
        });
        return;
    }
    
    // Find next camera
    const currentIndex = videoDevices.findIndex(device => device.deviceId === selectedDeviceId);
    const nextIndex = (currentIndex + 1) % videoDevices.length;
    selectedDeviceId = videoDevices[nextIndex].deviceId;
    
    // Restart camera with new device
    stopCamera();
    await initializeCamera();
});

// Stop camera function
function stopCamera() {
    if (currentStream) {
        currentStream.getTracks().forEach(track => track.stop());
        currentStream = null;
    }
    const video = document.getElementById('previewCamera');
    video.srcObject = null;
}

// Handle manual search
document.getElementById('searchVehicle').addEventListener('click', function() {
    const vehicleNumber = document.getElementById('vehicleNumber').value;
    if (vehicleNumber) {
        searchVehicle(vehicleNumber);
    }
});

// Search vehicle function
function searchVehicle(vehicleNumber) {
    fetch(`/api/Parking/GetVehicleDetails?vehicleNumber=${encodeURIComponent(vehicleNumber)}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateParkingDetails(data.vehicle);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: data.message || 'Vehicle not found'
                });
                clearForm();
            }
        })
        .catch(error => {
            console.error('Error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to fetch vehicle details'
            });
        });
}

// Update parking details in the form
function updateParkingDetails(vehicle) {
    document.getElementById('entryTime').textContent = new Date(vehicle.entryTime).toLocaleString();
    
    // Calculate duration
    const duration = calculateDuration(new Date(vehicle.entryTime), new Date());
    document.getElementById('duration').textContent = duration;
    
    // Update rate and total amount
    document.getElementById('rate').textContent = `$${vehicle.hourlyRate}/hr`;
    document.getElementById('totalAmount').textContent = `$${vehicle.totalAmount}`;
}

// Calculate duration between two dates
function calculateDuration(startDate, endDate) {
    const diff = endDate - startDate;
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours} hrs ${minutes} mins`;
}

// Process exit button handler
document.getElementById('processExit').addEventListener('click', function() {
    const vehicleNumber = document.getElementById('vehicleNumber').value;
    if (!vehicleNumber) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Please enter or scan a vehicle number'
        });
        return;
    }

    Swal.fire({
        title: 'Process Vehicle Exit?',
        text: 'Are you sure you want to process this vehicle exit?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, process exit',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            processVehicleExit(vehicleNumber);
        }
    });
});

// Process vehicle exit
async function processVehicleExit(vehicleNumber) {
    try {
        const response = await fetch('/api/Parking/ProcessExit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ vehicleNumber })
        });
        
        const data = await response.json();
        
        if (data.success) {
            Swal.fire({
                icon: 'success',
                title: 'Berhasil',
                text: 'Kendaraan berhasil keluar',
                timer: 1500,
                showConfirmButton: false
            }).then(() => {
                clearForm();
                updateRecentExits();
                
                // Restart scanner setelah berhasil
                if (isAutoProcessEnabled) {
                    document.getElementById('startButton').click();
                }
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: data.message || 'Gagal memproses keluar kendaraan'
            });
        }
    } catch (error) {
        console.error('Error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal memproses keluar kendaraan'
        });
    }
}

// Clear form button handler
document.getElementById('clear').addEventListener('click', clearForm);

// Clear form function
function clearForm() {
    document.getElementById('vehicleNumber').value = '';
    document.getElementById('entryTime').textContent = '--:--';
    document.getElementById('duration').textContent = '-- hrs -- mins';
    document.getElementById('rate').textContent = '$--/hr';
    document.getElementById('totalAmount').textContent = '$--';
}

// Update recent exits table
function updateRecentExits() {
    fetch('/api/Parking/GetRecentExits')
        .then(response => response.json())
        .then(data => {
            recentExitsTable.clear();
            data.forEach(exit => {
                recentExitsTable.row.add([
                    new Date(exit.exitTime).toLocaleString(),
                    exit.vehicleNumber,
                    exit.duration,
                    `$${exit.amount.toFixed(2)}`,
                    `<span class="badge bg-${exit.status === 'Paid' ? 'success' : 'warning'}">${exit.status}</span>`
                ]);
            });
            recentExitsTable.draw();
        })
        .catch(error => console.error('Error updating recent exits:', error));
}

// Initial load of recent exits
updateRecentExits();

// Refresh recent exits every minute
setInterval(updateRecentExits, 60000);