using System;
using IdentityServer4.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Persistence;
using Fhi.Smittestopp.Verification.Server.Credentials;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Fhi.Smittestopp.Verification.Server
{
    public class IdPortenConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string BaseUrl { get; set; }
    }

    public class ClientConfig
    {
        public string ClientId { get; set; }
        public string[] ClientSecretHashes { get; set; }
        public string[] RedirectUris { get; set; }
        public string[] AllowedScopes { get; set; }
        public string[] AllowedGrantTypes { get; set; }
        public string[] CorsOrigins { get; set; }
        public bool RequireConsent { get; set; }
        public bool RequireClientSecret { get; set; }
        public bool RequirePkce { get; set; }
    }

    public class HealthCheckConfig
    {
        public bool CheckDb { get; set; }
        public bool CheckMsis { get; set; }
    }

    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new[]
            { 
                new IdentityResources.OpenId(),
                new IdentityResource(VerificationScopes.VerificationInfo, new []
                {
                    JwtClaimTypes.Role,
                    VerificationClaims.VerifiedPositiveTestDate,
                    VerificationClaims.AnonymousToken
                })
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new []
            {
                new ApiScope(VerificationScopes.UploadApi, "Diagnosis keys upload API")
                {
                    UserClaims = new []
                    {
                        JwtClaimTypes.Role
                    }
                },
                new ApiScope(VerificationScopes.SkipMsisLookup, "Skip (do not perform) MSIS lookup")
                {
                    UserClaims = new []
                    {
                        VerificationClaims.SkipMsisLookup
                    }
                },
                new ApiScope(VerificationScopes.DkSmittestop, "Diagnosis keys upload API")
                {
                    UserClaims = new []
                    {
                        DkSmittestopClaims.Covid19Status,
                        DkSmittestopClaims.Covid19Blocked,
                        DkSmittestopClaims.Covid19InfectionStart,
                        DkSmittestopClaims.Covid19InfectionEnd,
                        DkSmittestopClaims.Covid19LimitCount,
                        DkSmittestopClaims.Covid19LimitDuration,
                        VerificationClaims.AnonymousToken,
                        JwtClaimTypes.Role
                    }
                }
            };

        public static Client CreateClientFromConfig(ClientConfig clientConfig)
        {
            return new Client
            {
                ClientId = clientConfig.ClientId,
                AllowedGrantTypes = clientConfig.AllowedGrantTypes,
                RequireClientSecret = clientConfig.RequireClientSecret,
                ClientSecrets = clientConfig.ClientSecretHashes?.Select(secretHash => new Secret(secretHash)).ToList(),
                AllowedScopes = clientConfig.AllowedScopes,
                RedirectUris = clientConfig.RedirectUris ?? new string[0],
                RequireConsent = clientConfig.RequireConsent,
                AllowedCorsOrigins = clientConfig.CorsOrigins ?? new string[0],
                RequirePkce = clientConfig.RequirePkce,
                AlwaysIncludeUserClaimsInIdToken = true,
                EnableLocalLogin = false,
                AllowOfflineAccess = false,
                IncludeJwtId = true
            };
        }
    }

    public static class ConfigExtensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration config)
        {
            return services.AddHealthChecks(config.Get<HealthCheckConfig>());
        }

        public static IServiceCollection AddHealthChecks(this IServiceCollection services, HealthCheckConfig config)
        {
            var hcBuilder = services.AddHealthChecks();

            if (config.CheckDb)
            {
                hcBuilder
                    .AddDbContextCheck<VerificationDbContext>()
                    .AddDbContextCheck<PersistedGrantDbContext>();
            }

            if (config.CheckMsis)
            {
                hcBuilder.AddCheck<MsisHealthCheck>("msis_health_check");
            }

            return services;
        }

        public static AuthenticationBuilder AddIdPortenAuth(this AuthenticationBuilder authBuilder, IConfiguration config)
        {
            return authBuilder.AddIdPortenAuth(config.Get<IdPortenConfig>());
        }

        public static AuthenticationBuilder AddIdPortenAuth(this AuthenticationBuilder authBuilder, IdPortenConfig config)
        {
            return authBuilder
                .AddOpenIdConnect(ExternalProviders.IdPorten, "ID-porten", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.MetadataAddress = config.BaseUrl + ".well-known/openid-configuration";

                    options.ResponseType = "code";

                    options.Authority = config.BaseUrl;
                    options.ClientId = config.ClientId;
                    options.ClientSecret = config.ClientSecret;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        // force reauthentication for each verification attempt
                        context.ProtocolMessage.Prompt = "login";
                        return Task.CompletedTask;
                    };
                });
        }

        public static IIdentityServerBuilder AddConfiguredClients(this IIdentityServerBuilder builder, IConfiguration config)
        {
            return builder.AddConfiguredClients(config.Get<ClientConfig[]>());
        }

        public static IIdentityServerBuilder AddConfiguredClients(this IIdentityServerBuilder builder, IEnumerable<ClientConfig> clientConfigs)
        {
            return builder.AddInMemoryClients(clientConfigs?.Select(Config.CreateClientFromConfig) ?? new Client[0]);
        }

        public static IIdentityServerBuilder AddSigningCredentialFromConfig(this IIdentityServerBuilder isBuilder, IConfiguration config)
        {
            if (config["useDevSigningCredentials"] == "True")
            {
                return isBuilder.AddDeveloperSigningCredential();
            }

            isBuilder.Services.Configure<SigningCredentialsStore.Config>(config);
            isBuilder.Services.AddSingleton<SigningCredentialsStore>();
            isBuilder.Services.AddSingleton<ISigningCredentialStore>(s => s.GetRequiredService<SigningCredentialsStore>());
            isBuilder.Services.AddSingleton<IValidationKeysStore>(s => s.GetRequiredService<SigningCredentialsStore>());
            return isBuilder;
        }

        public static IServiceCollection AddCertLocator(this IServiceCollection services, IConfiguration config)
        {
            switch (config["locator"])
            {
                case "local":
                {
                    return services.AddTransient<ICertificateLocator, LocalCertificateLocator>();
                }
                case "azure":
                {
                    return services
                        .Configure<AzureCertificateLocator.Config>(config.GetSection("azureVault"))
                        .AddTransient<ICertificateLocator, AzureCertificateLocator>();
                }
            }
            return services;
        }

        public static IServiceCollection ConfigureAuthCookies(this IServiceCollection services, IConfiguration config)
        {
            if (config["sameSiteLax"] == "True")
            {
                // Override SameSite=None with SameSite=Lax, to make modern browsers accept the cookies for http
                // NB! This does break silent token refresh, should that ever be needed.
                services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCookieAuthenticationScheme, options =>
                {
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });
                services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.ExternalCookieAuthenticationScheme, options =>
                {
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });
            }
            else
            {
                services.ConfigureSameSiteNoneWorkaround();
            }
            return services;
        }

        /// <summary>
        /// Adds handling of the issues regarding SameSite=None described here:
        /// https://itnext.io/user-agent-sniffing-only-way-to-deal-with-upcoming-samesite-cookie-changes-6f79a18e541
        /// https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureSameSiteNoneWorkaround(this IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
        }

        private static void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        /// <summary>
        /// This user agent sniffing should cover most browsers handling SameSite=None in an incompatible way
        /// </summary>
        /// <param name="userAgent">The user agent string to check SameSite=None behaviour for</param>
        /// <returns>True if provided userAgent matches known implemenations having an incompatible handling of SameSite=None</returns>
        private static bool DisallowsSameSiteNone(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            // Cover all iOS based browsers here. This includes:
            // - Safari on iOS 12 for iPhone, iPod Touch, iPad
            // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
            // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
            // All of which are broken by SameSite=None, because they use the iOS networking stack
            if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
            {
                return true;
            }

            // Cover Mac OS X based browsers that use the Mac OS networking stack. This includes:
            // - Safari on Mac OS X.
            // This does not include:
            // - Chrome on Mac OS X
            // Because they do not use the Mac OS networking stack.
            if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                userAgent.Contains("Version/") && userAgent.Contains("Safari"))
            {
                return true;
            }

            // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
            // and none in this range require it.
            // Note: this covers some pre-Chromium Edge versions, 
            // but pre-Chromium Edge does not require SameSite=None.
            if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            {
                return true;
            }

            return false;
        }
    }
}