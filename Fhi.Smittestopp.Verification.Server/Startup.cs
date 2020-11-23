using Fhi.Smittestopp.Verification.Domain;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Persistence;
using Fhi.Smittestopp.Verification.Server.Account;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fhi.Smittestopp.Verification.Server
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbHealthCheck(Configuration.GetConnectionString("verificationDb"));

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddApplicationInsightsTelemetry();

            services.AddControllersWithViews();

            services.AddMemoryCache();

            services.AddCertLocator(Configuration.GetSection("certificates"));

            services.AddIdentityServer(options =>
                {
                    // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddProfileService<ProfileService>()
                .AddConfiguredClients(Configuration.GetSection("clients"))
                .AddSigningCredentialFromConfig(Configuration.GetSection("signingCredentials"));

            services.AddMediatR(typeof(CreateFromExternalAuthentication).Assembly);

            services.AddTransient<IAccountService, AccountService>();

            services.AddDomainServices(Configuration.GetSection("common"));
            services.AddMockMsisLookup();
            services.AddPersistence(Configuration.GetConnectionString("verificationDb"));

            services.AddAuthentication()
                .AddIdPortenAuth(Configuration.GetSection("idPorten"));
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHealthChecks("/health");
            });

            // Ensure migrations for DB-context are applied
            app.MigrateDatabase<VerificationDbContext>();
        }
    }
}
