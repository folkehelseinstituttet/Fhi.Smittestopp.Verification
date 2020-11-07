using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Optional;
using Optional.Async.Extensions;

namespace Fhi.Smittestopp.Verification.Server.Credentials
{
    public class AzureCertificateLocator : ICertificateLocator
    {
        private readonly CertificateClient _certClient;
        private readonly SecretClient _secretClient;

        public AzureCertificateLocator(IOptions<Config> config)
        {
            TokenCredential azureCredentials = new DefaultAzureCredential();

            _certClient = new CertificateClient(vaultUri: new Uri(config.Value.VaultUri), credential: azureCredentials);
            _secretClient = new SecretClient(vaultUri: new Uri(config.Value.VaultUri), credential: azureCredentials);
        }

        public async Task<ICollection<CertificateVersion>> GetAllEnabledCertificateVersions(string certId)
        {
            // Get all the certificate versions
            var certVersionsPageable = _certClient.GetPropertiesOfCertificateVersionsAsync(certId);

            var enabledCertProps = new List<CertificateVersion>();
            await foreach (var certProps in certVersionsPageable)
            {
                if (certProps.Enabled == true && certProps.CreatedOn.HasValue)
                {
                    enabledCertProps.Add(new CertificateVersion
                    {
                        Certificate = await LoadCertificate(certProps),
                        Timestamp = certProps.CreatedOn.Value.UtcDateTime
                    });
                }
            }

            return enabledCertProps
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }

        public async Task<Option<X509Certificate2>> GetCertificate(string certId)
        {
            var certLookupResult = await _certClient.GetCertificateAsync(certId);

            return await certLookupResult.Value.SomeNotNull()
                .MapAsync(c => LoadCertificate(c.Properties));
        }

        private async Task<X509Certificate2> LoadCertificate(CertificateProperties item)
        {
            var certSecret = await _secretClient.GetSecretAsync(item.Name, item.Version);
            var privateKeyBytes = Convert.FromBase64String(certSecret.Value.Value);
            return new X509Certificate2(privateKeyBytes, (string)null, X509KeyStorageFlags.MachineKeySet);
        }

        public class Config
        {
            public string VaultUri { get; set; }
        }
    }
}