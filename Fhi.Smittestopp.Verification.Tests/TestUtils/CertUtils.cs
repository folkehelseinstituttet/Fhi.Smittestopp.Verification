using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fhi.Smittestopp.Verification.Tests.TestUtils
{
    public class CertUtils
    {
        public static X509Certificate2 GenerateTestEccCert()
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }

        public static X509Certificate2 GenerateTestRsaCert()
        {
            var rsa = RSA.Create();
            var req = new CertificateRequest("cn=foobar", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }
    }
}
