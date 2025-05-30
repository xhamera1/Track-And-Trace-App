using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace _10.Services
{
    /// <summary>
    /// Configuration options for the Nominatim geocoding service
    /// </summary>
    public class NominatimGeocodingOptions
    {
        /// <summary>
        /// Base URL for the Nominatim API (default: OpenStreetMap Nominatim)
        /// </summary>
        public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

        /// <summary>
        /// User agent string for API requests (required by Nominatim usage policy)
        /// </summary>
        public string UserAgent { get; set; } = "PackageTrackingApp/1.0";

        /// <summary>
        /// Email contact for API usage (recommended by Nominatim usage policy)
        /// </summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// Timeout for HTTP requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Whether to use HTTPS for requests
        /// </summary>
        public bool UseHttps { get; set; } = true;

        /// <summary>
        /// Maximum number of results to request from Nominatim
        /// </summary>
        public int MaxResults { get; set; } = 1;

        /// <summary>
        /// Language preference for results (ISO 639-1 language code)
        /// </summary>
        public string Language { get; set; } = "en";
    }

    /// <summary>
    /// Geocoding service implementation using the Nominatim API
    /// Follows OpenStreetMap Nominatim usage guidelines and best practices
    /// </summary>
    public class NominatimGeocodingService : IGeocodingService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimGeocodingService> _logger;
        private readonly NominatimGeocodingOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the NominatimGeocodingService
        /// </summary>
        /// <param name="httpClient">HTTP client for making API requests</param>
        /// <param name="logger">Logger for logging operations</param>
        /// <param name="options">Configuration options for the service</param>
        public NominatimGeocodingService(
            HttpClient httpClient,
            ILogger<NominatimGeocodingService> logger,
            IOptions<NominatimGeocodingOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            ConfigureHttpClient();
        }

        /// <summary>
        /// Geocodes an address to latitude and longitude coordinates
        /// </summary>
        public async Task<GeocodingResult> GeocodeAddressAsync(string street, string city, string zipCode, string country)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
                {
                    _logger.LogWarning("Geocoding failed: Both street and city cannot be empty");
                    return GeocodingResult.Failure("Either street or city must be provided for geocoding");
                }

                // Build the query string
                var queryComponents = new List<string>();

                if (!string.IsNullOrWhiteSpace(street))
                    queryComponents.Add(street.Trim());

                if (!string.IsNullOrWhiteSpace(city))
                    queryComponents.Add(city.Trim());

                if (!string.IsNullOrWhiteSpace(zipCode))
                    queryComponents.Add(zipCode.Trim());

                if (!string.IsNullOrWhiteSpace(country))
                    queryComponents.Add(country.Trim());

                var query = string.Join(", ", queryComponents);

                _logger.LogInformation("Attempting to geocode address: {Query}", query);

                // Build the request URL
                var requestUrl = BuildGeocodeUrl(query);

                // Make the API request
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogWarning("Received empty response from Nominatim API for query: {Query}", query);
                    return GeocodingResult.Failure("Received empty response from geocoding service");
                }

                // Parse the JSON response
                var results = JsonSerializer.Deserialize<NominatimGeocodeResponse[]>(responseContent, _jsonOptions);

                if (results == null || results.Length == 0)
                {
                    _logger.LogInformation("No geocoding results found for address: {Query}", query);
                    return GeocodingResult.Failure("No location found for the provided address");
                }

                // Get the best result (first one, as Nominatim returns them sorted by relevance)
                var bestResult = results[0];

                if (!decimal.TryParse(bestResult.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                    !decimal.TryParse(bestResult.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
                {
                    _logger.LogError("Failed to parse coordinates from Nominatim response: lat={Lat}, lon={Lon}",
                        bestResult.Lat, bestResult.Lon);
                    return GeocodingResult.Failure("Invalid coordinate data received from geocoding service");
                }

                _logger.LogInformation("Successfully geocoded address '{Query}' to coordinates: {Lat}, {Lon}",
                    query, latitude, longitude);

                return GeocodingResult.Success(
                    latitude,
                    longitude,
                    bestResult.DisplayName ?? query,
                    bestResult.Importance);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during geocoding request for address: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);
                return GeocodingResult.Failure($"Network error during geocoding: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Geocoding request timed out for address: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);
                return GeocodingResult.Failure("Geocoding request timed out");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse geocoding response for address: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);
                return GeocodingResult.Failure("Invalid response format from geocoding service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during geocoding for address: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);
                return GeocodingResult.Failure($"Unexpected error during geocoding: {ex.Message}");
            }
        }

        /// <summary>
        /// Reverse geocodes latitude and longitude coordinates to an address
        /// </summary>
        public async Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude)
        {
            try
            {
                // Validate coordinates
                if (latitude < -90 || latitude > 90)
                {
                    _logger.LogWarning("Invalid latitude value: {Latitude}", latitude);
                    return ReverseGeocodingResult.Failure("Latitude must be between -90 and 90 degrees");
                }

                if (longitude < -180 || longitude > 180)
                {
                    _logger.LogWarning("Invalid longitude value: {Longitude}", longitude);
                    return ReverseGeocodingResult.Failure("Longitude must be between -180 and 180 degrees");
                }

                _logger.LogInformation("Attempting to reverse geocode coordinates: {Lat}, {Lon}", latitude, longitude);

                // Build the request URL
                var requestUrl = BuildReverseGeocodeUrl(latitude, longitude);

                // Make the API request
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogWarning("Received empty response from Nominatim reverse geocoding API for coordinates: {Lat}, {Lon}",
                        latitude, longitude);
                    return ReverseGeocodingResult.Failure("Received empty response from reverse geocoding service");
                }

                // Parse the JSON response
                var result = JsonSerializer.Deserialize<NominatimReverseGeocodeResponse>(responseContent, _jsonOptions);

                if (result == null)
                {
                    _logger.LogInformation("No reverse geocoding results found for coordinates: {Lat}, {Lon}", latitude, longitude);
                    return ReverseGeocodingResult.Failure("No address found for the provided coordinates");
                }

                // Extract address components
                var address = result.Address;
                var street = ExtractStreetAddress(address);
                var city = ExtractCity(address);
                var zipCode = address?.Postcode;
                var country = address?.Country;

                _logger.LogInformation("Successfully reverse geocoded coordinates {Lat}, {Lon} to address: {DisplayName}",
                    latitude, longitude, result.DisplayName);

                return ReverseGeocodingResult.Success(
                    street,
                    city,
                    zipCode,
                    country,
                    result.DisplayName ?? $"{latitude}, {longitude}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during reverse geocoding request for coordinates: {Lat}, {Lon}",
                    latitude, longitude);
                return ReverseGeocodingResult.Failure($"Network error during reverse geocoding: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Reverse geocoding request timed out for coordinates: {Lat}, {Lon}",
                    latitude, longitude);
                return ReverseGeocodingResult.Failure("Reverse geocoding request timed out");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse reverse geocoding response for coordinates: {Lat}, {Lon}",
                    latitude, longitude);
                return ReverseGeocodingResult.Failure("Invalid response format from reverse geocoding service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during reverse geocoding for coordinates: {Lat}, {Lon}",
                    latitude, longitude);
                return ReverseGeocodingResult.Failure($"Unexpected error during reverse geocoding: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures the HTTP client with appropriate headers and settings
        /// </summary>
        private void ConfigureHttpClient()
        {
            // Set user agent (required by Nominatim usage policy)
            var userAgent = !string.IsNullOrWhiteSpace(_options.ContactEmail)
                ? $"{_options.UserAgent} ({_options.ContactEmail})"
                : _options.UserAgent;

            _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            // Set timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            _logger.LogDebug("Configured HTTP client with User-Agent: {UserAgent}, Timeout: {Timeout}s",
                userAgent, _options.TimeoutSeconds);
        }

        /// <summary>
        /// Builds the URL for geocoding requests
        /// </summary>
        private string BuildGeocodeUrl(string query)
        {
            var baseUrl = _options.UseHttps ? _options.BaseUrl.Replace("http://", "https://") : _options.BaseUrl;

            var encodedQuery = HttpUtility.UrlEncode(query);

            return $"{baseUrl}/search?q={encodedQuery}&format=json&limit={_options.MaxResults}&addressdetails=1&accept-language={_options.Language}";
        }

        /// <summary>
        /// Builds the URL for reverse geocoding requests
        /// </summary>
        private string BuildReverseGeocodeUrl(decimal latitude, decimal longitude)
        {
            var baseUrl = _options.UseHttps ? _options.BaseUrl.Replace("http://", "https://") : _options.BaseUrl;

            var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString("F6", CultureInfo.InvariantCulture);

            return $"{baseUrl}/reverse?lat={latStr}&lon={lonStr}&format=json&addressdetails=1&accept-language={_options.Language}";
        }

        /// <summary>
        /// Extracts street address from Nominatim address components
        /// </summary>
        private static string? ExtractStreetAddress(NominatimAddress? address)
        {
            if (address == null) return null;

            // Try different street address fields in order of preference
            return address.Road ??
                   address.HouseNumber + " " + address.Road ??
                   address.Pedestrian ??
                   address.Footway ??
                   address.Path;
        }

        /// <summary>
        /// Extracts city name from Nominatim address components
        /// </summary>
        private static string? ExtractCity(NominatimAddress? address)
        {
            if (address == null) return null;

            // Try different city fields in order of preference
            return address.City ??
                   address.Town ??
                   address.Village ??
                   address.Municipality ??
                   address.County;
        }

        /// <summary>
        /// Disposes the HTTP client and other resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Nominatim API Response Models

    /// <summary>
    /// Response model for Nominatim geocoding API
    /// </summary>
    internal class NominatimGeocodeResponse
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("importance")]
        public double? Importance { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    /// <summary>
    /// Response model for Nominatim reverse geocoding API
    /// </summary>
    internal class NominatimReverseGeocodeResponse
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    /// <summary>
    /// Address components model for Nominatim API responses
    /// </summary>
    internal class NominatimAddress
    {
        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; set; }

        [JsonPropertyName("road")]
        public string? Road { get; set; }

        [JsonPropertyName("pedestrian")]
        public string? Pedestrian { get; set; }

        [JsonPropertyName("footway")]
        public string? Footway { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }

    #endregion
}
