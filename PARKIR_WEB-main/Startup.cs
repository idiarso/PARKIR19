using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using ParkIRC.Services;

namespace ParkIRC
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Existing services...
            services.AddSingleton<PrinterService>();

            // Add security headers
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Add XSS protection
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "X-CSRF-TOKEN";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            // Add input validation
            services.AddFluentValidation(fv => 
                fv.RegisterValidatorsFromAssemblyContaining<Startup>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Existing configuration...
            var printerService = app.ApplicationServices.GetRequiredService<PrinterService>();
            printerService.InitializeAsync().Wait();

            // Add security headers middleware
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-always");
                await next();
            });
        }
    }
} 