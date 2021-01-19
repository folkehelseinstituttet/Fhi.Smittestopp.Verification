﻿using IdentityServer4.Models;
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
                        VerificationClaims.AnonymousToken
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
            return services;
        }
    }
}