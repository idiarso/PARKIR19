// Global variables
let entryScanner = null;
let exitScanner = null;
let entryCameraStream = null;
let exitCameraStream = null;
let selectedEntryCameraId = null;
let selectedExitCameraId = null;
let isAutoEntryEnabled = true;
let isAutoExitEnabled = true;

// Initialize DataTables
const recentEntriesTable = $('#recentEntriesTable').DataTable({
    order: [[0, 'desc']],
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/id.json'
    },
    pageLength: 5
});

const recentExitsTable = $('#recentExitsTable').DataTable({
    order: [[0, 'desc']],
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/id.json'
    },
    pageLength: 5
});

// SignalR connection for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/parkingHub")
    .withAutomaticReconnect()
    .build();

// Connect to SignalR hub
connection.start()
    .then(() => {
        console.log("Connected to ParkingHub");
        updateDashboardStats();
    })
    .catch(err => console.error("Error connecting to ParkingHub:", err));

// SignalR event handlers
connection.on("UpdateDashboardStats", updateDashboardStats);
connection.on("UpdateRecentEntries", updateRecentEntries);
connection.on("UpdateRecentExits", updateRecentExits);

// Auto switches handlers
document.getElementById('autoEntrySwitch').addEventListener('change', function() {
    isAutoEntryEnabled = this.checked;
});

document.getElementById('autoExitSwitch').addEventListener('change', function() {
    isAutoExitEnabled = this.checked;
});

// Initialize entry camera
const maxRetries = 3;
let retryCount = 0;
let retryDelay = 1000; // Start with 1 second delay
let isReconnecting = false;

async function initializeEntryCamera() {
    try {
        const video = document.getElementById('entryCamera');
        if (!video) {
            throw new Error('Camera element not found');
        }

        // IP Camera Configuration
        const ipCameraUrl = 'http://192.168.186.200:8000/stream';
        console.log('Menghubungkan ke kamera IP:', ipCameraUrl);

        if (Hls.isSupported()) {
            const hls = new Hls({
                debug: false,
                enableWorker: true,
                lowLatencyMode: true,
                backBufferLength: 90
            });

            hls.loadSource(ipCameraUrl);
            hls.attachMedia(video);

            hls.on(Hls.Events.MANIFEST_PARSED, function() {
                video.play().catch(e => {
                    console.error('Error playing video:', e);
                });
                retryCount = 0;
                retryDelay = 1000;
                isReconnecting = false;
            });

            hls.on(Hls.Events.ERROR, function(event, data) {
                if (data.fatal) {
                    switch(data.type) {
                        case Hls.ErrorTypes.NETWORK_ERROR:
                            handleNetworkError(hls);
                            break;
                        case Hls.ErrorTypes.MEDIA_ERROR:
                            handleMediaError(hls);
                            break;
                        default:
                            handleFatalError(hls);
                            break;
                    }
                }
            });
        } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
            video.src = ipCameraUrl;
            video.addEventListener('loadedmetadata', function() {
                video.play().catch(e => {
                    console.error('Error playing video:', e);
                    handleFatalError();
                });
            });
        } else {
            throw new Error('HLS tidak didukung oleh browser ini');
        }

        entryCameraStream = video.srcObject;
        startMotionDetection();
    } catch (error) {
        console.error('Error mengakses kamera:', error);
        showErrorAlert('Error', 'Gagal mengakses kamera: ' + error.message);
    }
}

function handleNetworkError(hls) {
    if (!isReconnecting && retryCount < maxRetries) {
        isReconnecting = true;
        console.log(`Error jaringan, mencoba ulang ${retryCount + 1} dari ${maxRetries} dalam ${retryDelay/1000} detik...`);
        
        showReconnectingAlert(retryCount + 1);
        
        setTimeout(() => {
            hls.startLoad();
            retryCount++;
            retryDelay *= 2; // Exponential backoff
            isReconnecting = false;
        }, retryDelay);
    } else if (retryCount >= maxRetries) {
        console.error('Batas maksimum percobaan tercapai');
        showMaxRetriesAlert();
        hls.destroy();
    }
}

function handleMediaError(hls) {
    console.log('Error media, mencoba memulihkan...');
    showMediaErrorAlert();
    hls.recoverMediaError();
}

function handleFatalError(hls) {
    console.error('Error tidak dapat dipulihkan');
    showFatalErrorAlert();
    if (hls) hls.destroy();
}

function showReconnectingAlert(attempt) {
    Swal.fire({
        icon: 'warning',
        title: 'Koneksi Terputus',
        text: `Mencoba menghubungkan kembali ke kamera... (Percobaan ${attempt}/${maxRetries})`,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

function showMaxRetriesAlert() {
    Swal.fire({
        icon: 'error',
        title: 'Error Koneksi',
        text: 'Gagal terhubung ke kamera setelah beberapa percobaan. Silakan periksa koneksi jaringan dan kamera.',
        showConfirmButton: true
    });
}

function showMediaErrorAlert() {
    Swal.fire({
        icon: 'warning',
        title: 'Error Media',
        text: 'Mencoba memulihkan stream kamera...',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

function showFatalErrorAlert() {
    Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Gagal terhubung ke kamera. Silakan periksa koneksi dan refresh halaman.',
        showConfirmButton: true
    });
}

function showErrorAlert(title, message) {
    Swal.fire({
        icon: 'error',
        title: title,
        text: message
    });
}

// Initialize exit camera and barcode scanner
async function initializeExitCamera() {
    try {
        exitScanner = new Html5Qrcode("interactive");
        const devices = await Html5Qrcode.getCameras();

        if (devices && devices.length) {
            selectedExitCameraId = selectedExitCameraId || devices[0].id;
            await startScanning();
        } else {
            throw new Error('Tidak ada kamera yang ditemukan');
        }
    } catch (error) {
        console.error('Error inisialisasi scanner:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal menginisialisasi scanner: ' + error.message
        });
    }
}

// Start barcode scanning
async function startScanning() {
    try {
        await exitScanner.start(
            selectedExitCameraId,
            {
                fps: 10,
                qrbox: { width: 250, height: 250 },
                formatsToSupport: [
                    Html5QrcodeSupportedFormats.QR_CODE,
                    Html5QrcodeSupportedFormats.CODE_128,
                    Html5QrcodeSupportedFormats.CODE_39,
                    Html5QrcodeSupportedFormats.EAN_13
                ]
            },
            handleScannedCode,
            handleScanError
        );

        document.getElementById('startScanButton').style.display = 'none';
        document.getElementById('stopScanButton').style.display = 'inline-block';

        // Show scanning status
        Swal.fire({
            icon: 'info',
            title: 'Scanner Aktif',
            text: 'Arahkan barcode ke kamera',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000
        });
    } catch (error) {
        console.error('Error starting scanner:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Gagal memulai scanner: ' + error.message
        });
    }
}

// Handle scanned barcode
async function handleScannedCode(decodedText) {
    try {
        // Play beep sound
        const audio = new Audio('/audio/beep.mp3');
        audio.play();

        if (isAutoExitEnabled) {
            // Show loading
            const loadingAlert = Swal.fire({
                title: 'Memproses...',
                text: 'Mohon tunggu sebentar',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Process exit
            const response = await fetch('/Parking/ProcessExit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(decodedText)
            });

            const data = await response.json();
            await loadingAlert.close();

            if (data.success) {
                updateExitDetails(data);
                await Swal.fire({
                    icon: 'success',
                    title: 'Berhasil',
                    text: data.message,
                    timer: 1500,
                    showConfirmButton: false
                });

                // Restart scanner if auto process is enabled
                if (isAutoExitEnabled) {
                    await startScanning();
                }
            } else {
                throw new Error(data.message);
            }
        } else {
            // Just update the form with scanned data
            document.getElementById('vehicleNumber').value = decodedText;
            await searchVehicle(decodedText);
        }
    } catch (error) {
        console.error('Error processing exit:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'Gagal memproses kendaraan keluar'
        });
    }
}

// Handle scan errors
function handleScanError(error) {
    console.warn('Scan error:', error);
}

// Motion detection for entry camera
function startMotionDetection() {
    const video = document.getElementById('entryCamera');
    const canvas = document.getElementById('entryCanvas');
    const ctx = canvas.getContext('2d');
    const roiBox = document.querySelector('.roi-box');
    let lastImageData = null;
    const motionThreshold = 30;
    const motionPixels = 1000;

    function detectMotion() {
        if (!entryCameraStream) return;

        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;

        if (lastImageData) {
            let motionCount = 0;
            for (let i = 0; i < data.length; i += 4) {
                const diff = Math.abs(data[i] - lastImageData.data[i]) +
                            Math.abs(data[i+1] - lastImageData.data[i+1]) +
                            Math.abs(data[i+2] - lastImageData.data[i+2]);
                if (diff > motionThreshold) {
                    motionCount++;
                }
            }

            if (motionCount > motionPixels) {
                roiBox.classList.add('motion-detected');
                if (isAutoEntryEnabled) {
                    captureVehicle();
                }
            } else {
                roiBox.classList.remove('motion-detected');
            }
        }

        lastImageData = imageData;
        requestAnimationFrame(detectMotion);
    }

    video.addEventListener('play', () => {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        detectMotion();
    });
}

// Capture vehicle photo
async function captureVehicle() {
    const video = document.getElementById('entryCamera');
    const canvas = document.getElementById('entryCanvas');
    const ctx = canvas.getContext('2d');

    // Draw current frame to canvas
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

    // Convert to base64
    const base64Image = canvas.toDataURL('image/jpeg');

    // Add visual feedback
    document.querySelector('.roi-box').classList.add('capture-highlight');
    setTimeout(() => {
        document.querySelector('.roi-box').classList.remove('capture-highlight');
    }, 1000);

    return base64Image;
}

// Entry form submit handler
document.getElementById('entryForm').addEventListener('submit', async function(e) {
    e.preventDefault();

    try {
        const base64Image = await captureVehicle();
        const formData = {
            vehicleNumber: document.getElementById('vehicleNumber').value,
            vehicleType: document.getElementById('vehicleType').value,
            base64Image: base64Image
        };

        const response = await fetch('/Parking/ProcessEntry', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(formData)
        });

        const data = await response.json();

        if (data.success) {
            // Show ticket modal
            document.getElementById('ticketContent').innerHTML = data.ticketHtml;
            const ticketModal = new bootstrap.Modal(document.getElementById('ticketModal'));
            ticketModal.show();

            // Clear form
            this.reset();
        } else {
            throw new Error(data.message);
        }
    } catch (error) {
        console.error('Error processing entry:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'Gagal memproses kendaraan masuk'
        });
    }
});

// Print ticket handler
document.getElementById('printTicket').addEventListener('click', function() {
    window.print();
});

// Update dashboard stats
async function updateDashboardStats() {
    try {
        const response = await fetch('/Parking/GetDashboardStats');
        const data = await response.json();

        document.getElementById('activeVehicles').textContent = data.activeVehicles;
        document.getElementById('availableSlots').textContent = data.availableSlots;
        document.getElementById('todayTransactions').textContent = data.todayTransactions;
        document.getElementById('todayRevenue').textContent = `Rp ${data.todayRevenue.toLocaleString()}`;
    } catch (error) {
        console.error('Error updating dashboard stats:', error);
    }
}

// Update recent entries
function updateRecentEntries(entries) {
    recentEntriesTable.clear();
    entries.forEach(entry => {
        recentEntriesTable.row.add([
            entry.timestamp,
            entry.vehicleNumber,
            entry.vehicleType,
            `<span class="badge bg-success">Active</span>`
        ]);
    });
    recentEntriesTable.draw();
}

// Update recent exits
function updateRecentExits(exits) {
    recentExitsTable.clear();
    exits.forEach(exit => {
        recentExitsTable.row.add([
            exit.exitTime,
            exit.vehicleNumber,
            exit.duration,
            `Rp ${exit.totalAmount.toLocaleString()}`
        ]);
    });
    recentExitsTable.draw();
}

// Initialize everything when document is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeEntryCamera();
    initializeExitCamera();
    updateDashboardStats();
    
    // Update stats every minute
    setInterval(updateDashboardStats, 60000);
});