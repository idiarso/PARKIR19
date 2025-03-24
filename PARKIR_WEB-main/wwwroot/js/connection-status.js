const connectionStatus = {
    init() {
        this.indicator = document.getElementById('connection-indicator');
        this.setupSignalR();
        this.startLocalConnectionCheck();
    },

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/parkingHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("SystemStatusChanged", (status) => {
            this.updateStatus(status);
        });

        this.connection.start().catch(err => {
            console.error('SignalR Connection Error:', err);
            this.updateStatus({ isConnected: false, error: 'Server tidak dapat dijangkau' });
        });
    },

    updateStatus(status) {
        if (status.isConnected) {
            this.indicator.classList.remove('bg-danger', 'bg-warning');
            this.indicator.classList.add('bg-success');
            this.indicator.title = 'Sistem Berjalan Normal';
        } else {
            this.indicator.classList.remove('bg-success');
            this.indicator.classList.add('bg-danger');
            
            let errorMsg = 'Sistem Terputus:\n';
            if (!status.database) errorMsg += '- Database tidak terhubung\n';
            if (!status.printer) errorMsg += '- Printer tidak terhubung\n';
            if (!status.camera) errorMsg += '- Kamera tidak terhubung\n';
            if (status.error) errorMsg += `- Error: ${status.error}`;
            
            this.indicator.title = errorMsg;
        }
    },

    startLocalConnectionCheck() {
        setInterval(() => {
            fetch('/api/system/status', { method: 'GET' })
                .catch(() => {
                    this.updateStatus({ 
                        isConnected: false, 
                        error: 'Server aplikasi tidak dapat dijangkau'
                    });
                });
        }, 30000); // Check every 30 seconds
    }
};

document.addEventListener('DOMContentLoaded', () => {
    connectionStatus.init();
}); 