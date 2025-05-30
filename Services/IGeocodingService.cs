using System.Threading.Tasks;

namespace _10.Services
{
    /// <summary>
    /// Service interface for geocoding operations using external APIs
    /// </summary>
    public interface IGeocodingService
    {
        /// <summary>
        /// Geocodes an address to latitude and longitude coordinates
        /// </summary>
        /// <param name="street">Street address</param>
        /// <param name="city">City name</param>
        /// <param name="zipCode">Postal/ZIP code</param>
        /// <param name="country">Country name or code</param>
        /// <returns>Geocoding result with coordinates or error information</returns>
        Task<GeocodingResult> GeocodeAddressAsync(string street, string city, string zipCode, string country);

        /// <summary>
        /// Reverse geocodes latitude and longitude coordinates to an address
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <returns>Reverse geocoding result with address information</returns>
        Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude);
    }

    /// <summary>
    /// Result of a geocoding operation
    /// </summary>
    public class GeocodingResult
    {
        /// <summary>
        /// Whether the geocoding operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Latitude coordinate (if successful)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate (if successful)
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Error message (if operation failed)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Display name of the found location
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Confidence level of the geocoding result (0-1)
        /// </summary>
        public double? Confidence { get; set; }

        /// <summary>
        /// Creates a successful geocoding result
        /// </summary>
        public static GeocodingResult Success(decimal latitude, decimal longitude, string displayName, double? confidence = null)
        {
            return new GeocodingResult
            {
                IsSuccess = true,
                Latitude = latitude,
                Longitude = longitude,
                DisplayName = displayName,
                Confidence = confidence
            };
        }

        /// <summary>
        /// Creates a failed geocoding result
        /// </summary>
        public static GeocodingResult Failure(string errorMessage)
        {
            return new GeocodingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Result of a reverse geocoding operation
    /// </summary>
    public class ReverseGeocodingResult
    {
        /// <summary>
        /// Whether the reverse geocoding operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Street address (if successful)
        /// </summary>
        public string? Street { get; set; }

        /// <summary>
        /// City name (if successful)
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// ZIP/Postal code (if successful)
        /// </summary>
        public string? ZipCode { get; set; }

        /// <summary>
        /// Country name (if successful)
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Full display name of the location
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Error message (if operation failed)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful reverse geocoding result
        /// </summary>
        public static ReverseGeocodingResult Success(string? street, string? city, string? zipCode,
            string? country, string displayName)
        {
            return new ReverseGeocodingResult
            {
                IsSuccess = true,
                Street = street,
                City = city,
                ZipCode = zipCode,
                Country = country,
                DisplayName = displayName
            };
        }

        /// <summary>
        /// Creates a failed reverse geocoding result
        /// </summary>
        public static ReverseGeocodingResult Failure(string errorMessage)
        {
            return new ReverseGeocodingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
