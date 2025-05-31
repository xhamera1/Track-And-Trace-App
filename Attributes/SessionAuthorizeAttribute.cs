using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _10.Attributes
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public string[]? RequiredRoles { get; set; }

        public SessionAuthorizeAttribute() { } 

        public SessionAuthorizeAttribute(params string[] roles)
        {
            RequiredRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (RequiredRoles != null && RequiredRoles.Length > 0)
            {
                var userRole = context.HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userRole) || !RequiredRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
