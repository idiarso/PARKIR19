/**
 * Currency formatting utilities for the application
 */

/**
 * Formats a number as Indonesian Rupiah
 * @param {number} amount - The amount to format
 * @returns {string} - Formatted string in Indonesian Rupiah
 */
function formatRupiah(amount) {
    return new Intl.NumberFormat('id-ID', { 
        style: 'currency', 
        currency: 'IDR',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

/**
 * Parses a Rupiah formatted string back to a number
 * @param {string} rupiahString - The Rupiah formatted string
 * @returns {number} - The parsed number
 */
function parseRupiah(rupiahString) {
    if (!rupiahString) return 0;
    // Remove currency symbol, dots, and replace comma with dot
    return parseFloat(
        rupiahString
            .replace(/[^\d,]/g, '')  // Remove all non-digits and non-commas
            .replace(',', '.')       // Replace comma with dot for decimal
    );
} 