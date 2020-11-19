using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Interfaces
{
    public interface ICertificateLocator
    {
        Task<ICollection<CertificateVersion>> GetAllEnabledCertificateVersionsAsync(string certId);
        Task<Option<X509Certificate2>> GetCertificateAsync(string certId);
        Option<X509Certificate2> GetCertificate(string certId);
    }
}