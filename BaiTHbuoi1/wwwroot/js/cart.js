document.addEventListener('DOMContentLoaded', function () {
    // Handle quantity changes
    const quantityInputs = document.querySelectorAll('.quantity-input');
    quantityInputs.forEach(input => {
        input.addEventListener('change', function () {
            if (this.value < 1) {
                this.value = 1;
            }
        });
    });

    // Auto-submit form when quantity changes (optional)
    const autoUpdateCart = false; // Set to true if you want auto-update
    if (autoUpdateCart) {
        quantityInputs.forEach(input => {
            input.addEventListener('change', function () {
                document.getElementById('cart-form').submit();
            });
        });
    }
});