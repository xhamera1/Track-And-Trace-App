using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;

namespace _10.Attributes
{
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

                httpContext.Items["ApiUser"] = user;

                logger.LogInformation("API authentication successful for admin user: {Username}", user.Username);

                await next();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during API authentication for API key: {ApiKeyPrefix}***",
                    GetApiKeyPrefix(apiKey));
                context.Result = CreateSystemErrorResult(SYSTEM_ERROR_MESSAGE);
            }
        }


        private static string? ExtractApiKey(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.ContainsKey(API_KEY_HEADER))
            {
                return httpContext.Request.Headers[API_KEY_HEADER].FirstOrDefault();
            }


            var headerKey = httpContext.Request.Headers.Keys
                .FirstOrDefault(k => string.Equals(k, API_KEY_HEADER, StringComparison.OrdinalIgnoreCase));

            return headerKey != null ? httpContext.Request.Headers[headerKey].FirstOrDefault() : null;
        }


        private static async Task<User?> GetUserByApiKeyAsync(ApplicationDbContext dbContext, string apiKey)
        {
            return await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        }


        private static bool IsAdminUser(User user)
        {
            return user.Role == UserRole.Admin;
        }


        private static string GetApiKeyPrefix(string apiKey)
        {
            return apiKey.Length > 8 ? apiKey[..8] : apiKey;
        }

        private static UnauthorizedObjectResult CreateUnauthorizedResult(string message, string? details = null)
        {
            var result = new { message, details };
            return new UnauthorizedObjectResult(details != null ? result : new { message });
        }


        private static ObjectResult CreateForbiddenResult(string message)
        {
            return new ObjectResult(new { message })
            {
                StatusCode = 403
            };
        }


        private static ObjectResult CreateSystemErrorResult(string message)
        {
            return new ObjectResult(new { message })
            {
                StatusCode = 500
            };
        }
    }
}
