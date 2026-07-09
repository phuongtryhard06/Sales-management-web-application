using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FreshMart.Middleware
{
    public class AdminAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Protect admin routes except login
            if (path != null && path.StartsWith("/admin") && !path.Contains("login"))
            {
                var userId = context.Session.GetInt32("UserId");
                var userRole = context.Session.GetString("UserRole");

                if (userId == null)
                {
                    context.Response.Redirect("/User/Login");
                    return;
                }

                if (userRole != "Admin")
                {
                    // Logged in but not Admin => Access Denied
                    context.Response.Redirect("/Home/AccessDenied");
                    return;
                }
            }

            await _next(context);
        }
    }
}
