using System;
using System.Security.Policy;
using Fhi.Smittestopp.Verification.Domain;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Persistence;
using Fhi.Smittestopp.Verification.Server.Account;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Optional;

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
                .AddDbHealthCheck(Configuration.GetConnectionString("verificationDb"))
                .AddCheck<MsisHealthCheck>("msis_health_check");

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddControllersWithViews();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            });

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

            // Add MediatR and all handlers from specified assemplies
            services.AddMediatR(typeof(CreateFromExternalAuthentication).Assembly);

            services.AddTransient<IAccountService, AccountService>();

            services.AddDomainServices(Configuration.GetSection("common"));

            services.AddMsisLookup(Configuration.GetSection("msis"));

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

            // override host and scheme to fixed values in request context regardless of request headers
            var fixedHost = Configuration["common:fixedHost"]
                .SomeWhen(x => !string.IsNullOrEmpty(x))
                .Map(x => new Uri(x));
            fixedHost.MatchSome(fixedHostUri =>
            {
                app.Use(async (ctx, next) =>
                {
                    ctx.Request.Scheme = fixedHostUri.Scheme;
                    ctx.Request.Host = new HostString(fixedHostUri.Host);

                    await next();
                });
            });

            app.UseForwardedHeaders();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

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
