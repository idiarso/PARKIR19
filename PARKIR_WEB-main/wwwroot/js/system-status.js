const systemStatus = {
    connection: null,
    
    init() {
        this.setupSignalR();
        this.setupUI();
    },
    
    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/parkinghub")
            .withAutomaticReconnect()
            .build();
        
        // Receive system status updates
        this.connection.on("ReceiveSystemStatus", (status) => {
            this.updateStatusIndicators(status);
            this.updateSystemResources(status);
        });
        
        // Receive notifications
        this.connection.on("ReceiveNotification", (type, message) => {
            this.showNotification(type, message);
        });
        
        // Receive errors
        this.connection.on("ReceiveError", (message) => {
            this.showNotification("error", message);
        });
        
        // Start connection
        this.connection.start()
            .then(() => {
                console.log("SignalR connected");
                this.refreshStatus();
            })
            .catch(err => {
                console.error("SignalR connection error: ", err);
                setTimeout(() => this.connection.start(), 5000);
            });
    },
    
    setupUI() {
        // Setup refresh button if it exists
        const refreshBtn = document.getElementById('refresh-status');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.refreshStatus());
        }
        
        // Setup auto-refresh if on system status page
        if (document.getElementById('system-status-page')) {
            setInterval(() => this.refreshStatus(), 60000); // Refresh every minute
        }
    },
    
    refreshStatus() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("RequestSystemStatus").catch(err => {
                console.error("Error requesting system status: ", err);
            });
        }
    },
    
    updateStatusIndicators(status) {
        // Update status indicators if they exist
        this.updateIndicator('database-status', status.database);
        this.updateIndicator('printer-status', status.printer);
        this.updateIndicator('camera-status', status.camera);
        
        // Update header status indicator
        const headerIndicator = document.getElementById('connection-indicator');
        if (headerIndicator) {
            if (status.isConnected) {
                headerIndicator.classList.remove('bg-danger', 'bg-warning');
                headerIndicator.classList.add('bg-success');
                headerIndicator.title = 'All systems connected';
            } else {
                headerIndicator.classList.remove('bg-success', 'bg-warning');
                headerIndicator.classList.add('bg-danger');
                headerIndicator.title = 'Connection issues detected';
            }
        }
    },
    
    updateIndicator(elementId, isConnected) {
        const indicator = document.getElementById(elementId);
        if (!indicator) return;
        
        if (isConnected) {
            indicator.classList.remove('badge-danger');
            indicator.classList.add('badge-success');
            indicator.textContent = 'Connected';
        } else {
            indicator.classList.remove('badge-success');
            indicator.classList.add('badge-danger');
            indicator.textContent = 'Disconnected';
        }
    },
    
    updateSystemResources(status) {
        // Update disk usage
        this.updateResourceBar('disk-usage-bar', status.diskUsagePercent);
        this.updateResourceText('disk-total', status.diskTotal);
        this.updateResourceText('disk-used', status.diskUsed);
        this.updateResourceText('disk-free', status.diskFree);
        
        // Update memory usage
        if (status.memoryTotal && status.memoryUsed) {
            const memUsed = parseFloat(status.memoryUsed.replace('G', ''));
            const memTotal = parseFloat(status.memoryTotal.replace('G', ''));
            const memPercent = Math.round((memUsed / memTotal) * 100);
            this.updateResourceBar('memory-usage-bar', memPercent + '%');
        }
        this.updateResourceText('memory-total', status.memoryTotal);
        this.updateResourceText('memory-used', status.memoryUsed);
        this.updateResourceText('memory-free', status.memoryFree);
    },
    
    updateResourceBar(elementId, percentage) {
        const bar = document.getElementById(elementId);
        if (!bar) return;
        
        const percent = parseInt(percentage);
        bar.style.width = percentage;
        bar.textContent = percentage;
        
        // Update color based on usage
        if (percent > 80) {
            bar.classList.remove('bg-success', 'bg-warning');
            bar.classList.add('bg-danger');
        } else if (percent > 60) {
            bar.classList.remove('bg-success', 'bg-danger');
            bar.classList.add('bg-warning');
        } else {
            bar.classList.remove('bg-warning', 'bg-danger');
            bar.classList.add('bg-success');
        }
    },
    
    updateResourceText(elementId, value) {
        const element = document.getElementById(elementId);
        if (element && value) {
            element.textContent = value;
        }
    },
    
    showNotification(type, message) {
        // Check if toastr is available
        if (typeof toastr !== 'undefined') {
            switch (type.toLowerCase()) {
                case 'error':
                    toastr.error(message);
                    break;
                case 'warning':
                    toastr.warning(message);
                    break;
                case 'success':
                    toastr.success(message);
                    break;
                default:
                    toastr.info(message);
            }
        } else {
            // Fallback to alert if toastr is not available
            alert(`${type.toUpperCase()}: ${message}`);
        }
    }
};

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', () => {
    systemStatus.init();
}); 