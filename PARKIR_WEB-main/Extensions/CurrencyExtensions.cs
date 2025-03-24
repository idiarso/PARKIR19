using System.Globalization;

namespace ParkIRC.Extensions
{
    public static class CurrencyExtensions
    {
        // Indonesian culture for Rupiah formatting
        private static readonly CultureInfo IDCulture = new CultureInfo("id-ID");
        
        /// <summary>
        /// Formats a decimal value as Indonesian Rupiah
        /// </summary>
        /// <param name="value">The decimal value to format</param>
        /// <returns>A string formatted as Indonesian Rupiah</returns>
        public static string ToRupiah(this decimal value)
        {
            return string.Format(IDCulture, "Rp {0:N0}", value);
        }
        
        /// <summary>
        /// Formats a nullable decimal value as Indonesian Rupiah
        /// </summary>
        /// <param name="value">The nullable decimal value to format</param>
        /// <returns>A string formatted as Indonesian Rupiah, or empty string if null</returns>
        public static string ToRupiah(this decimal? value)
        {
            if (value.HasValue)
                return ToRupiah(value.Value);
            
            return string.Empty;
        }
        
        /// <summary>
        /// Formats a double value as Indonesian Rupiah
        /// </summary>
        /// <param name="value">The double value to format</param>
        /// <returns>A string formatted as Indonesian Rupiah</returns>
        public static string ToRupiah(this double value)
        {
            return ToRupiah((decimal)value);
        }
        
        /// <summary>
        /// Formats a nullable double value as Indonesian Rupiah
        /// </summary>
        /// <param name="value">The nullable double value to format</param>
        /// <returns>A string formatted as Indonesian Rupiah, or empty string if null</returns>
        public static string ToRupiah(this double? value)
        {
            if (value.HasValue)
                return ToRupiah((decimal)value.Value);
            
            return string.Empty;
        }
    }
} 