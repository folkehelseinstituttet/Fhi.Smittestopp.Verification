using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
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

    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            { 
                new IdentityResources.OpenId()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new []
            {
                new ApiScope("upload-api", "Diagnosis keys upload API")
            };

        public static IEnumerable<Client> Clients =>
            new []
            {
                new Client
                {
                    ClientId = "test-spa-client",
                    AllowedGrantTypes = GrantTypes.Code,
                    ClientSecrets =
                    {
                        new Secret("dummy".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = { "openid", "upload-api" },

                    RedirectUris = new []
                    {
                        "http://localhost:4200/"
                    }
                }
            };

        public static AuthenticationBuilder AddIdPortenAuth(this AuthenticationBuilder authBuilder, IConfiguration config)
        {
            return authBuilder.AddIdPortenAuth(config.Get<IdPortenConfig>());
        }

        public static AuthenticationBuilder AddIdPortenAuth(this AuthenticationBuilder authBuilder, IdPortenConfig config)
        {
            return authBuilder
                .AddOpenIdConnect("idporten", "ID-porten", options =>
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
                }); ;
        }
    }
}