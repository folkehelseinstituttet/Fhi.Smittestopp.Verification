using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class RollingKeyPairGenerator
    {
        private readonly byte[] _masterKeyBytes;
        private readonly X9ECParameters _ecParameters;

        public RollingKeyPairGenerator(byte[] masterKeyBytes, X9ECParameters ecParameters)
        {
            _masterKeyBytes = masterKeyBytes;
            _ecParameters = ecParameters;
        }

        public RollingKeyPairGenerator(X509Certificate2 certificate, X9ECParameters ecParameters)
            : this(RetrievePrivateKeyBytes(certificate), ecParameters)
        {
        }

        private static byte[] RetrievePrivateKeyBytes(X509Certificate2 certificate)
        {
            var ecdsaPrivateKey = certificate.GetECDsaPrivateKey();
            if (ecdsaPrivateKey != null)
            {
                return ecdsaPrivateKey.ExportParameters(true).D;
            }

            var rsaPrivateKey = certificate.GetRSAPrivateKey();
            if (rsaPrivateKey != null)
            {
                // TODO: find a better way to extract bytes for RSA key
                return Encoding.UTF8.GetBytes(rsaPrivateKey.ToXmlString(true));
            }

            throw new NotSupportedException($"Unsupported private key for certificate {certificate.Thumbprint}");
        }

        public (BigInteger privateKey, ECPublicKeyParameters publicKey) GenerateKeyPairForInterval(long keyIntervalNumber)
        {
            var privateKey = GeneratePrivateKey(_masterKeyBytes, keyIntervalNumber, _ecParameters);
            var publicKey = CalculatePublicKey(privateKey, _ecParameters);
            return (privateKey, publicKey);
        }

        private static BigInteger GeneratePrivateKey(byte[] masterKeyBytes, long keyIntervalNumber, X9ECParameters ecParameters)
        {
            var keyByteSize = (int) Math.Ceiling(ecParameters.Curve.Order.BitCount / 8.0);
            var counter = 0;

            BigInteger privateKey;
            do
            {
                var privateKeyBytes = GeneratePrivateKeyBytes(keyByteSize, masterKeyBytes, keyIntervalNumber, counter);
                privateKey = new BigInteger(privateKeyBytes);
                counter++;
                if (counter > 1000)
                {
                    throw new GeneratePrivateKeyException("Maximum number of iterations exceeded while generating private key within curve order.");
                }
            } while (privateKey.CompareTo(ecParameters.Curve.Order) > 0); // Private key must be within curve order

            return privateKey;
        }

        private static byte[] GeneratePrivateKeyBytes(int numBytes, byte[] masterKeyBytes, long keyIntervalNumber, int counter)
        {
            var keyIntervalBytes = BitConverter.GetBytes(keyIntervalNumber);
            var counterBytes = BitConverter.GetBytes(counter);
            var ikm = masterKeyBytes;
            var salt = keyIntervalBytes.Concat(counterBytes).ToArray();
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());
            var hParams = new HkdfParameters(ikm, salt, null);
            hkdf.Init(hParams);
            byte[] keyBytes = new byte[numBytes];
            hkdf.GenerateBytes(keyBytes, 0, numBytes);
            return keyBytes;
        }

        private static ECPublicKeyParameters CalculatePublicKey(BigInteger privateKey, X9ECParameters ecParameters)
        {
            var publicKeyPoint = ecParameters.G.Multiply(privateKey);
            var domainParams = new ECDomainParameters(ecParameters);
            return new ECPublicKeyParameters("ECDSA", publicKeyPoint, domainParams);
        }
    }

    public class GeneratePrivateKeyException : Exception
    {
        public GeneratePrivateKeyException(string message) : base(message)
        {
        }
    }
}
