using System;
using System.Net.Http;
using System.Security.Authentication;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Utilities;
using Fhi.Smittestopp.Verification.Msis.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Msis
{
    public class MsisConfig
    {
        public string BaseUrl { get; set; }
        public bool Mock { get; set; }
        /// <summary>
        /// Cert thumbprint if using local certificate store, Cert name if using Azure key vault.
        /// </summary>
        public string CertId { get; set; }
    }

    public interface IMsisClientCertLocator : ICachedCertLocator
    {
    }

    public class MsisClientCertLocator : CachedCertLocator, IMsisClientCertLocator
    {
        public MsisClientCertLocator(IOptions<Config> config, ICertificateLocator certLocator, IMemoryCache cache)
        : base(config.Value.CertId, nameof(MsisClientCertLocator), certLocator, cache)
        {
        }

        public class Config
        {
            public string CertId { get; set; }
        }
    }

    public static class MsisConfigExtensions
    {
        public static IServiceCollection AddMsisLookup(this IServiceCollection services, IConfiguration config)
        {
            return services.AddMsisLookup(config.Get<MsisConfig>());
        }

        public static IServiceCollection AddMsisLookup(this IServiceCollection services, MsisConfig config)
        {
            services.AddTransient<IMsisLookupService, MsisLookupService>();
            services.Configure<MsisClientCertLocator.Config>(c => c.CertId = config.CertId);
            services.AddTransient<IMsisClientCertLocator, MsisClientCertLocator>();

            if (config.Mock)
            {
                services.AddTransient<IMsisClient, MockMsisClient>();
            }
            else
            {
                services.AddHttpClient<IMsisClient, MsisClient>(c =>
                    {
                        c.BaseAddress = new Uri(config.BaseUrl);
                        c.DefaultRequestHeaders.Add("Accept", "application/json");
                    })
                    .ConfigurePrimaryHttpMessageHandler(s =>
                    {
                        var cert = s.GetService<IMsisClientCertLocator>().GetCertificate();
                        var handler = new HttpClientHandler
                        {
                            ClientCertificateOptions = ClientCertificateOption.Manual,
                            SslProtocols = SslProtocols.Tls12
                        };
                        handler.ClientCertificates.Add(cert);
                        return handler;
                    });
            }

            return services;
        }
    }
}
