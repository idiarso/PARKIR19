const entryGateOperator = {
    // Format plat nomor otomatis
    autoFormatPlate(input) {
        let value = input.value.toUpperCase();
        value = value.replace(/[^A-Z0-9 ]/g, '');
        
        // Format: B 1234 ABC
        if (value.length > 0) {
            // Area code (1-2 huruf)
            if (value.length <= 2) {
                input.value = value;
            }
            // Spasi setelah area code
            else if (value.length > 2) {
                let formatted = value.charAt(0);
                if (value.length > 1) formatted += value.charAt(1);
                formatted += ' ';
                
                // Angka (1-4 digit)
                let numbers = value.substring(2).match(/\d+/);
                if (numbers) formatted += numbers[0];
                
                // Spasi dan huruf akhir
                let letters = value.substring(2).match(/[A-Z]+/);
                if (letters) formatted += ' ' + letters[0];
                
                input.value = formatted;
            }
        }
    },

    // Quick buttons untuk jenis kendaraan
    setVehicleType(type) {
        document.getElementById('vehicleType').value = type;
        // Highlight selected button
        document.querySelectorAll('.vehicle-type-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-type="${type}"]`).classList.add('active');
    },

    // Capture dan preview image
    async captureImage() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            const video = document.getElementById('camera-feed');
            video.srcObject = stream;
            
            // Capture setelah delay untuk fokus
            await new Promise(resolve => setTimeout(resolve, 1000));
            
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            canvas.getContext('2d').drawImage(video, 0, 0);
            
            // Convert ke base64
            const imageData = canvas.toDataURL('image/jpeg');
            document.getElementById('capturedImage').src = imageData;
            
            // Stop camera
            stream.getTracks().forEach(track => track.stop());
            
            return imageData;
        } catch (err) {
            console.error('Error capturing image:', err);
            alert('Gagal mengambil gambar. Pastikan kamera terhubung.');
        }
    },

    // Update recent entries
    updateRecentEntries(entries) {
        const tbody = document.getElementById('recentEntries');
        tbody.innerHTML = entries.map(entry => `
            <tr>
                <td>${new Date(entry.entryTime).toLocaleString()}</td>
                <td>${entry.ticketNumber}</td>
                <td>${entry.vehicleNumber}</td>
                <td>${entry.vehicleType}</td>
                <td>
                    <a href="javascript:void(0)" onclick="showImage('${entry.imagePath}')">
                        <img src="/images/entries/${entry.imagePath}" 
                             class="img-thumbnail" width="50">
                    </a>
                </td>
            </tr>
        `).join('');
    }
}; 