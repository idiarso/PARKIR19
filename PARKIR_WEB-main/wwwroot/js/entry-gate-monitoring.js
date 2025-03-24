const entryGateMonitoring = {
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
        
        // Monitor entry events
        this.connection.on("EntryButtonPressed", (entryPoint) => {
            this.logEvent(`Push button pressed at gate ${entryPoint}`);
            this.updatePushButtonStatus(entryPoint, true);
        });
        
        this.connection.on("TriggerCamera", (entryPoint) => {
            this.logEvent(`Camera triggered at gate ${entryPoint}`);
        });
        
        this.connection.on("PrintTicket", (ticketData) => {
            this.logEvent(`Ticket printed: ${ticketData.ticketNumber} for ${ticketData.vehicleNumber}`);
        });
        
        this.connection.on("OpenEntryGate", (entryPoint) => {
            this.logEvent(`Gate ${entryPoint} opened`);
            this.updateGateStatus(entryPoint, true);
            
            // After delay, close gate
            setTimeout(() => {
                this.updateGateStatus(entryPoint, false);
                this.updatePushButtonStatus(entryPoint, false);
                this.logEvent(`Gate ${entryPoint} closed automatically`);
            }, 5000);
        });
        
        this.connection.on("GateStatusChanged", (gateId, isOpen) => {
            this.updateGateStatus(gateId, isOpen);
        });
        
        this.connection.start()
            .then(() => console.log("SignalR connected for monitoring"))
            .catch(err => console.error("SignalR connection error: ", err));
    },
    
    setupEventListeners() {
        // Refresh button
        const refreshBtn = document.getElementById('refresh-gates');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.refreshGateStatus());
        }
        
        // Simulate push button
        const simulateBtn = document.getElementById('simulate-push-button');
        if (simulateBtn) {
            simulateBtn.addEventListener('click', () => {
                const gateSelect = document.getElementById('entry-gate-select');
                const entryPoint = gateSelect ? gateSelect.value : 'ENTRY1';
                this.simulatePushButton(entryPoint);
            });
        }
    },
    
    refreshGateStatus() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("GetAllGateStatus")
                .then(gates => {
                    this.updateAllGates(gates);
                })
                .catch(err => {
                    console.error("Error getting gate status: ", err);
                    this.logEvent("Error refreshing gate status", "error");
                });
        }
    },
    
    simulatePushButton(entryPoint) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.logEvent(`Simulating push button press at gate ${entryPoint}...`);
            
            this.connection.invoke("PushButtonPressed", entryPoint)
                .catch(err => {
                    console.error("Error simulating push button: ", err);
                    this.logEvent("Error simulating push button", "error");
                });
        }
    },
    
    updateAllGates(gates) {
        // Update gates in the table
        const gateRows = document.querySelectorAll('table tbody tr');
        gates.forEach(gate => {
            const row = Array.from(gateRows).find(row => row.cells[0].textContent === gate.name);
            if (row) {
                // Update status
                row.cells[1].innerHTML = gate.isOnline ? 
                    '<span class="badge bg-success">Online</span>' : 
                    '<span class="badge bg-danger">Offline</span>';
                
                // Update gate
                row.cells[2].innerHTML = gate.isOpen ? 
                    '<span class="badge bg-warning">Open</span>' : 
                    '<span class="badge bg-secondary">Closed</span>';
                
                // Update last activity
                row.cells[4].textContent = new Date(gate.lastActivity).toLocaleTimeString();
            }
        });
    },
    
    updateGateStatus(gateId, isOpen) {
        // Find the gate row and update it
        const gateRows = document.querySelectorAll('table tbody tr');
        const row = Array.from(gateRows).find(row => row.cells[0].textContent.includes(gateId));
        
        if (row) {
            // Update gate status
            row.cells[2].innerHTML = isOpen ? 
                '<span class="badge bg-warning">Open</span>' : 
                '<span class="badge bg-secondary">Closed</span>';
            
            // Update last activity
            row.cells[4].textContent = new Date().toLocaleTimeString();
        }
    },
    
    updatePushButtonStatus(gateId, isPressed) {
        const pushButtonStatus = document.getElementById('push-button-status');
        if (!pushButtonStatus) return;
        
        // Find if there's already a status for this gate
        const existingStatus = pushButtonStatus.querySelector(`[data-gate="${gateId}"]`);
        
        if (existingStatus) {
            // Update existing status
            existingStatus.className = `alert alert-${isPressed ? 'warning' : 'secondary'}`;
            existingStatus.innerHTML = `
                <i class="fas fa-hand-pointer me-2"></i>
                Push Button ${gateId}: ${isPressed ? 'Pressed' : 'Ready'}
            `;
        } else {
            // Create new status
            const newStatus = document.createElement('div');
            newStatus.className = `alert alert-${isPressed ? 'warning' : 'secondary'} mb-2`;
            newStatus.setAttribute('data-gate', gateId);
            newStatus.innerHTML = `
                <i class="fas fa-hand-pointer me-2"></i>
                Push Button ${gateId}: ${isPressed ? 'Pressed' : 'Ready'}
            `;
            pushButtonStatus.appendChild(newStatus);
        }
    },
    
    logEvent(message, type = 'info') {
        const simulationLog = document.getElementById('simulation-log');
        if (!simulationLog) return;
        
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('p');
        logEntry.className = `mb-1 text-${type === 'error' ? 'danger' : type === 'warning' ? 'warning' : 'dark'}`;
        logEntry.innerHTML = `<small>[${timestamp}]</small> ${message}`;
        
        simulationLog.prepend(logEntry);
        
        // Limit log size
        if (simulationLog.children.length > 50) {
            simulationLog.removeChild(simulationLog.lastChild);
        }
    }
};

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', () => {
    entryGateMonitoring.init();
}); 