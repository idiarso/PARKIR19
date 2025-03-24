// Data Clearing Form
document.getElementById('clearPeriod')?.addEventListener('change', function() {
    const customDateGroup = document.getElementById('customDateGroup');
    if (this.value === 'custom') {
        customDateGroup.classList.remove('d-none');
    } else {
        customDateGroup.classList.add('d-none');
    }
});

document.getElementById('clearDataForm')?.addEventListener('submit', async function(e) {
    e.preventDefault();

    const clearOptions = Array.from(this.querySelectorAll('input[name="clearOptions[]"]:checked'))
        .map(cb => cb.value);

    if (clearOptions.length === 0) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Pilih minimal satu jenis data yang akan dihapus'
        });
        return;
    }

    const result = await Swal.fire({
        icon: 'warning',
        title: 'Konfirmasi Penghapusan Data',
        text: 'Data yang dihapus tidak dapat dikembalikan. Pastikan Anda telah membuat backup sebelum melanjutkan. Apakah Anda yakin ingin menghapus data yang dipilih?',
        showCancelButton: true,
        confirmButtonText: 'Ya, Hapus Data',
        cancelButtonText: 'Batal',
        confirmButtonColor: '#dc3545'
    });

    if (!result.isConfirmed) return;

    const formData = new FormData(this);

    try {
        const loadingAlert = Swal.fire({
            title: 'Menghapus Data...',
            text: 'Mohon tunggu sebentar',
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        const response = await fetch('/Parking/ClearData', {
            method: 'POST',
            body: formData
        });

        const data = await response.json();
        await loadingAlert.close();

        if (data.success) {
            await Swal.fire({
                icon: 'success',
                title: 'Berhasil',
                text: data.message
            });
            this.reset();
            document.getElementById('customDateGroup').classList.add('d-none');
        } else {
            throw new Error(data.message);
        }
    } catch (error) {
        console.error('Error:', error);
        await Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'Terjadi kesalahan saat menghapus data'
        });
    }
}); 