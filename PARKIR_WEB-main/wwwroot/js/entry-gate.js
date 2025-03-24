const entryGateSystem = {
    connection: null,
    
    init() {
        this.setupSignalR();
        this.setupEventListeners();
    },
    
    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/parkinghub")
            .withAutomaticReconnect()
            .build();
        
        // Listen for push button events
        this.connection.on("EntryButtonPressed", (entryPoint) => {
            console.log(`Push button pressed at ${entryPoint}`);
            this.showStatus(`Processing entry at gate ${entryPoint}...`);
        });
        
        // Listen for camera trigger events
        this.connection.on("TriggerCamera", (entryPoint) => {
            this.captureVehicleImage(entryPoint);
        });
        
        // Listen for printer events
        this.connection.on("PrintTicket", (ticketData) => {
            this.updateTicketDisplay(ticketData);
        });
        
        // Listen for gate events
        this.connection.on("OpenEntryGate", (entryPoint) => {
            this.updateGateStatus(entryPoint, true);
        });
        
        this.connection.start()
            .then(() => console.log("SignalR connected for entry gate system"))
            .catch(err => console.error("SignalR connection error: ", err));
    },
    
    setupEventListeners() {
        // Simulate push button press (for testing UI)
        const testButton = document.getElementById('test-push-button');
        if (testButton) {
            testButton.addEventListener('click', () => {
                this.simulatePushButtonPress('ENTRY1');
            });
        }
    },
    
    simulatePushButtonPress(entryPoint) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("PushButtonPressed", entryPoint)
                .catch(err => console.error("Error simulating push button press: ", err));
        }
    },
    
    captureVehicleImage(entryPoint) {
        // In production, this would connect to camera hardware
        // For simulation, show a status update
        this.showStatus("Capturing vehicle image...");
        
        // Simulate a delay for image capture
        setTimeout(() => {
            const ticketData = {
                ticketNumber: "PK-" + Math.floor(100000 + Math.random() * 900000),
                entryTime: new Date().toLocaleString(),
                entryPoint: entryPoint,
                vehicleType: Math.random() > 0.5 ? "Motor" : "Mobil",
                plateNumber: this.generateRandomPlate()
            };
            
            // Print ticket with the data
            this.printTicket(ticketData);
        }, 1500);
    },
    
    printTicket(ticketData) {
        this.showStatus("Printing ticket...");
        
        // Send print command to server
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("PrintEntryTicket", ticketData)
                .then(() => {
                    // Update UI with ticket info
                    this.updateTicketDisplay(ticketData);
                    
                    // Simulate gate opening after ticket is printed
                    setTimeout(() => {
                        this.openGate(ticketData.entryPoint);
                    }, 2000);
                })
                .catch(err => {
                    console.error("Error printing ticket: ", err);
                    this.showStatus("Error printing ticket", "error");
                });
        }
    },
    
    openGate(entryPoint) {
        this.showStatus("Opening gate...");
        
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("OpenEntryGate", entryPoint)
                .then(() => {
                    this.updateGateStatus(entryPoint, true);
                    
                    // Simulate gate closing after a delay
                    setTimeout(() => {
                        this.updateGateStatus(entryPoint, false);
                        this.showStatus("Ready for next vehicle", "success", 3000);
                    }, 5000);
                })
                .catch(err => {
                    console.error("Error opening gate: ", err);
                    this.showStatus("Error opening gate", "error");
                });
        }
    },
    
    updateTicketDisplay(ticketData) {
        const ticketDisplay = document.getElementById('ticket-display');
        if (!ticketDisplay) return;
        
        ticketDisplay.innerHTML = `
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h5 class="mb-0">Tiket Parkir</h5>
                </div>
                <div class="card-body">
                    <div class="text-center mb-3">
                        <i class="fas fa-ticket-alt fa-3x text-primary"></i>
                    </div>
                    
                    <div class="ticket-content">
                        <p class="mb-1"><strong>No. Tiket:</strong> ${ticketData.ticketNumber}</p>
                        <p class="mb-1"><strong>Waktu Masuk:</strong> ${ticketData.entryTime}</p>
                        <p class="mb-1"><strong>Jenis Kendaraan:</strong> ${ticketData.vehicleType}</p>
                        <p class="mb-0"><strong>Plat Nomor:</strong> ${ticketData.plateNumber}</p>
                    </div>
                    
                    <div class="text-center mt-3">
                        <img src="data:image/png;base64,${this.generateBarcode(ticketData.ticketNumber)}" 
                             alt="Barcode" class="img-fluid" style="max-width: 200px;">
                    </div>
                    
                    <div class="small text-center text-muted mt-2">
                        Simpan tiket ini dengan baik
                    </div>
                </div>
            </div>
        `;
    },
    
    updateGateStatus(entryPoint, isOpen) {
        const gateStatus = document.getElementById('gate-status');
        if (!gateStatus) return;
        
        gateStatus.innerHTML = `
            <div class="alert alert-${isOpen ? 'success' : 'secondary'}">
                <i class="fas fa-door-${isOpen ? 'open' : 'closed'} me-2"></i>
                Gate ${entryPoint}: ${isOpen ? 'Terbuka' : 'Tertutup'}
            </div>
        `;
        
        if (isOpen) {
            // Play gate open sound
            const audio = new Audio('/sounds/gate-open.mp3');
            audio.play().catch(e => console.log('Error playing sound'));
        }
    },
    
    showStatus(message, type = 'info', duration = 0) {
        const statusDisplay = document.getElementById('status-display');
        if (!statusDisplay) return;
        
        statusDisplay.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show">
                <i class="fas fa-info-circle me-2"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        
        if (duration > 0) {
            setTimeout(() => {
                $(statusDisplay).find('.alert').alert('close');
            }, duration);
        }
    },
    
    generateRandomPlate() {
        const letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const regions = ["B", "D", "F", "AB", "AD", "AE", "Z", "R"];
        const region = regions[Math.floor(Math.random() * regions.length)];
        const numbers = Math.floor(1000 + Math.random() * 9000);
        const suffix = letters.charAt(Math.floor(Math.random() * letters.length)) + 
                      letters.charAt(Math.floor(Math.random() * letters.length)) + 
                      letters.charAt(Math.floor(Math.random() * letters.length));
        
        return `${region} ${numbers} ${suffix}`;
    },
    
    generateBarcode(text) {
        // This would be a real barcode generation in production
        // For now, return a placeholder
        return "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";
    }
};

// Initialize when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    entryGateSystem.init();
}); 