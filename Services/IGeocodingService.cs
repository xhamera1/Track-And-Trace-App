using System.Threading.Tasks;

namespace _10.Services
{
    public interface IGeocodingService
    {

        Task<GeocodingResult> GeocodeAddressAsync(string street, string city, string zipCode, string country);

        Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude);
    }

    public class GeocodingResult
    {

        public bool IsSuccess { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? ErrorMessage { get; set; }

        public string? DisplayName { get; set; }

        public double? Confidence { get; set; }

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

        public static GeocodingResult Failure(string errorMessage)
        {
            return new GeocodingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
    public class ReverseGeocodingResult
    {
        public bool IsSuccess { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? ZipCode { get; set; }

        public string? Country { get; set; }

        public string? DisplayName { get; set; }

        public string? ErrorMessage { get; set; }

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
