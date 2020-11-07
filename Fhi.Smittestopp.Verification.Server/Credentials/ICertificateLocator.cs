using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.Credentials
{
    public interface ICertificateLocator
    {
        Task<ICollection<CertificateVersion>> GetAllEnabledCertificateVersions(string certId);
        Task<Option<X509Certificate2>> GetCertificate(string certId);
    }
}