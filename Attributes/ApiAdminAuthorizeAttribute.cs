using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;

namespace _10.Attributes
{
    /// <summary>
    /// Authorization attribute for API endpoints that require admin access with API key authentication via HTTP header
    /// </summary>
    public class ApiAdminAuthorizeAttribute : ActionFilterAttribute
    {
        private const string API_KEY_HEADER = "X-API-Key";
        private const string UNAUTHORIZED_MESSAGE = "Invalid or missing API key.";
        private const string FORBIDDEN_MESSAGE = "API key does not have admin privileges.";
        private const string SYSTEM_ERROR_MESSAGE = "Authentication failed due to system error.";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var serviceProvider = httpContext.RequestServices;
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApiAdminAuthorizeAttribute>>();

            // Extract API key from HTTP header
            var apiKey = ExtractApiKey(httpContext);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                context.Result = CreateUnauthorizedResult("API key is required in header.",
                    $"Include '{API_KEY_HEADER}' header in your request.");
                return;
            }

            try
            {
                var user = await GetUserByApiKeyAsync(dbContext, apiKey);

                if (user == null)
                {
                    logger.LogWarning("API authentication failed for API key: {ApiKeyPrefix}***", GetApiKeyPrefix(apiKey));
                    context.Result = CreateUnauthorizedResult(UNAUTHORIZED_MESSAGE);
                    return;
                }

                if (!IsAdminUser(user))
                {
                    logger.LogWarning("API access denied for non-admin user: {Username} with role: {Role}",
                        user.Username, user.Role);
                    context.Result = CreateForbiddenResult(FORBIDDEN_MESSAGE);
                    return;
                }

                // Store user information in HttpContext for use in action methods
                httpContext.Items["ApiUser"] = user;

                logger.LogInformation("API authentication successful for admin user: {Username}", user.Username);

                // Continue to the action method
                await next();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during API authentication for API key: {ApiKeyPrefix}***",
                    GetApiKeyPrefix(apiKey));
                context.Result = CreateSystemErrorResult(SYSTEM_ERROR_MESSAGE);
            }
        }

        /// <summary>
        /// Extracts the API key from the request headers
        /// </summary>
        private static string? ExtractApiKey(HttpContext httpContext)
        {
            // Try case-sensitive first, then case-insensitive
            if (httpContext.Request.Headers.ContainsKey(API_KEY_HEADER))
            {
                return httpContext.Request.Headers[API_KEY_HEADER].FirstOrDefault();
            }

            // Case-insensitive fallback
            var headerKey = httpContext.Request.Headers.Keys
                .FirstOrDefault(k => string.Equals(k, API_KEY_HEADER, StringComparison.OrdinalIgnoreCase));

            return headerKey != null ? httpContext.Request.Headers[headerKey].FirstOrDefault() : null;
        }

        /// <summary>
        /// Retrieves user by API key from database
        /// </summary>
        private static async Task<User?> GetUserByApiKeyAsync(ApplicationDbContext dbContext, string apiKey)
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        }

        /// <summary>
        /// Checks if the user has admin role
        /// </summary>
        private static bool IsAdminUser(User user)
        {
            return user.Role == UserRole.Admin;
        }

        /// <summary>
        /// Gets the first few characters of API key for logging (security)
        /// </summary>
        private static string GetApiKeyPrefix(string apiKey)
        {
            return apiKey.Length > 8 ? apiKey[..8] : apiKey;
        }

        /// <summary>
        /// Creates a standardized unauthorized result
        /// </summary>
        private static UnauthorizedObjectResult CreateUnauthorizedResult(string message, string? details = null)
        {
            var result = new { message, details };
            return new UnauthorizedObjectResult(details != null ? result : new { message });
        }

        /// <summary>
        /// Creates a standardized forbidden result
        /// </summary>
        private static ObjectResult CreateForbiddenResult(string message)
        {
            return new ObjectResult(new { message })
            {
                StatusCode = 403
            };
        }

        /// <summary>
        /// Creates a standardized system error result
        /// </summary>
        private static ObjectResult CreateSystemErrorResult(string message)
        {
            return new ObjectResult(new { message })
            {
                StatusCode = 500
            };
        }
    }
}
