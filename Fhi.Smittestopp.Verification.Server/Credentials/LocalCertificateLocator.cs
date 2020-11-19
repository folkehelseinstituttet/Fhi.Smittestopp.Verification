using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.Credentials
{
    public class LocalCertificateLocator : ICertificateLocator
    {
        public Task<ICollection<CertificateVersion>> GetAllEnabledCertificateVersionsAsync(string certId)
        {
            var certVersions = FindCertByThumbprint(certId)
                .Map(c => new List<CertificateVersion>
                {
                    new CertificateVersion
                    {
                        Timestamp = DateTime.Now,
                        Certificate = c
                    }
                })
                .ValueOr(() => new List<CertificateVersion>());

            return Task.FromResult<ICollection<CertificateVersion>>(certVersions);
        }

        public Task<Option<X509Certificate2>> GetCertificateAsync(string certId)
        {
            return Task.FromResult(GetCertificate(certId));
        }

        public Option<X509Certificate2> GetCertificate(string certId)
        {
            return FindCertByThumbprint(certId);
        }

        private static Option<X509Certificate2> FindCertByThumbprint(string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            var enumerator = certCollection.GetEnumerator();
            var cert = Option.None<X509Certificate2>();
            while (enumerator.MoveNext())
            {
                cert = enumerator.Current.Some();
            }
            store.Close();
            return cert;
        }
    }
}