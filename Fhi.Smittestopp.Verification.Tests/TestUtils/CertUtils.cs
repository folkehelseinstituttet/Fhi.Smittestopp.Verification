using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Smittestopp.Verification.Tests.TestUtils
{
    public class CertUtils
    {
        public static X509Certificate2 GenerateTestCert()
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }
    }
}
