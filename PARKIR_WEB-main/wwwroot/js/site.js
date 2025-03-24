// Funciones para mejorar la interfaz de usuario y validación

$(document).ready(function () {
    // Auto-cerrar las alertas después de 5 segundos
    setTimeout(function () {
        $('.alert-success, .alert-info').fadeOut('slow');
    }, 5000);

    // Mejorar la validación del campo de teléfono
    $.validator.addMethod("phoneID", function (value, element) {
        if (value === "") {
            return true; // Permitir el valor vacío
        }
        return /^(\+62|62|0)[0-9]{9,13}$/.test(value);
    }, "Format nomor telepon tidak valid. Gunakan format: +628xxx, 08xxx, atau 628xxx");

    if ($.validator && $.validator.unobtrusive) {
        $.validator.unobtrusive.adapters.add("phoneID", [], function (options) {
            options.rules.phoneID = true;
            options.messages.phoneID = options.message;
        });
    }

    // Validación adicional para contraseñas
    $.validator.addMethod("passwordStrength", function (value, element) {
        return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{6,100}$/.test(value);
    }, "Password harus mengandung huruf kecil, huruf besar, angka, dan karakter khusus");

    // Inicializar tooltips y popovers de Bootstrap
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Toggle para mostrar/ocultar contraseñas
    $('.toggle-password').on('click', function () {
        var targetId = $(this).data('target');
        var inputField = $('#' + targetId);
        var icon = $(this).find('i');
        
        if (inputField.attr('type') === 'password') {
            inputField.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            inputField.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    // Formato de número de teléfono automático
    $('input[type="tel"]').on('input', function() {
        var phoneNumber = $(this).val().replace(/\D/g, '');
        if (phoneNumber.length > 0 && phoneNumber.substring(0, 1) !== '0' && phoneNumber.substring(0, 2) !== '62') {
            phoneNumber = '0' + phoneNumber;
        }
        $(this).val(phoneNumber);
    });
}); 