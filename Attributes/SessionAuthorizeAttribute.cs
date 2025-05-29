using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _10.Attributes
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public string[]? RequiredRoles { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                // User is not logged in, redirect to login
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if specific roles are required
            if (RequiredRoles != null && RequiredRoles.Length > 0)
            {
                var userRole = context.HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userRole) || !RequiredRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                {
                    // User doesn't have required role, redirect to access denied or home
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
