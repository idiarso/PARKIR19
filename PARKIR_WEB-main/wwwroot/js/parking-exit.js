$(document).ready(function() {
    let currentTransaction = null;

    // Handler untuk scan barcode
    $('#barcodeInput').on('change', function() {
        let barcode = $(this).val();
        processScannedTicket(barcode);
    });

    function processScannedTicket(barcode) {
        $.post('/Parking/ProcessExitTicket', { barcode: barcode })
            .done(function(response) {
                if (response.success) {
                    currentTransaction = response;
                    $('#vehicleNumber').val(response.vehicleNumber);
                    $('#entryPhoto').attr('src', response.entryPhoto);
                    $('#totalAmount').val(response.amount);
                    $('#exitModal').modal('show');
                } else {
                    alert(response.message);
                }
            });
    }

    // Handler untuk proses keluar
    $('#btnProcessExit').click(function() {
        if (!currentTransaction) return;

        let data = {
            transactionNumber: currentTransaction.transactionNumber,
            paymentAmount: $('#totalAmount').val(),
            paymentMethod: $('#paymentMethod').val()
        };

        $.post('/Parking/CompleteExit', data)
            .done(function(response) {
                if (response.success) {
                    printExitTicket(response.receiptData);
                    $('#exitModal').modal('hide');
                    currentTransaction = null;
                } else {
                    alert(response.message);
                }
            });
    });

    function printExitTicket(data) {
        // Format data untuk printer
        let printContent = `
            TIKET PARKIR - KELUAR
            ==================
            
            No. Transaksi: ${data.transactionNumber}
            No. Kendaraan: ${data.vehicleNumber}
            
            Waktu Masuk: ${formatDateTime(data.entryTime)}
            Waktu Keluar: ${formatDateTime(data.exitTime)}
            Durasi: ${formatDuration(data.duration)}
            
            Total Biaya: Rp ${formatNumber(data.amount)}
            Metode Bayar: ${data.paymentMethod}
            
            ==================
            Terima Kasih
            `;

        // Kirim ke printer menggunakan PrintService
        PrintService.print({
            content: printContent,
            printerName: 'EPSON_TM_T82', // Sesuaikan dengan nama printer yang terkonfigurasi
            copies: 1,
            paperSize: '80mm',
            paperCut: true
        }).catch(error => {
            console.error('Error printing:', error);
            alert('Gagal mencetak tiket: ' + error.message);
        });
    }

    function formatDateTime(date) {
        return new Date(date).toLocaleString('id-ID', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function formatDuration(hours) {
        const h = Math.floor(hours);
        const m = Math.round((hours - h) * 60);
        return `${h} jam ${m} menit`;
    }

    function formatNumber(num) {
        return num.toLocaleString('id-ID');
    }
}); 