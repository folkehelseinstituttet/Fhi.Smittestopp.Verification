using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
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

    public interface IMsisClientCertLocator
    {
        X509Certificate2 GetClientCert();
    }

    public class MsisClientCertLocator : IMsisClientCertLocator
    {
        private const string MemoryCacheKey = "MsisClientCert";

        private readonly string _certId;

        private readonly ICertificateLocator _certLocator;
        private readonly IMemoryCache _cache;

        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);

        public MsisClientCertLocator(IOptions<Config> config, ICertificateLocator certLocator, IMemoryCache cache)
        {
            _certLocator = certLocator;
            _cache = cache;
            _certId = config.Value.CertId;
        }

        public X509Certificate2 GetClientCert()
        {
            _cacheLock.Wait();
            try
            {
                return _cache.GetOrCreate(MemoryCacheKey, (cache) =>
                {
                    cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
                    return _certLocator.GetCertificate(_certId);
                }).ValueOr(() => throw new Exception("Unable to locate MSIS client certificate for ID: " + _certId));
            }
            finally
            {
                _cacheLock.Release();
            }
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
                        var cert = s.GetService<IMsisClientCertLocator>().GetClientCert();
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
