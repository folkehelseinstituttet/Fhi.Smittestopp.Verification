using System;
using Fhi.Smittestopp.Verification.Persistence;
using Fhi.Smittestopp.Verification.Server.Account.ViewModels;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Server
{
    /// <summary>
    /// Extensions taken from the Identity Server Quickstart
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Checks if the redirect URI is for a native client.
        /// </summary>
        /// <returns></returns>
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
               && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";
            
            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }

        public static IApplicationBuilder MigrateDatabase<T>(this IApplicationBuilder app) where T : DbContext
        {
            using var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            try
            {
                scope.ServiceProvider.GetRequiredService<T>().Database.Migrate();
            }
            catch (Exception e)
            {
                scope.ServiceProvider.GetRequiredService<ILogger<Startup>>().LogError(e, "Failed to migrate database for " + typeof(T).Name);
            }
            return app;
        }
    }
}
