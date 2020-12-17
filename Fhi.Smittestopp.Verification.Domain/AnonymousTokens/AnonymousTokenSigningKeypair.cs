using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenSigningKeypair
    {
        public AnonymousTokenSigningKeypair(string kid,
            string curveName,
            X9ECParameters ecParameters,
            BigInteger privateKey,
            ECPublicKeyParameters publicKey)
        {
            Kid = kid;
            CurveName = curveName;
            PrivateKey = privateKey;
            PublicKey = publicKey;
            EcParameters = ecParameters;
        }

        public string Kid { get; set; }
        public string CurveName { get; set; }
        public BigInteger PrivateKey { get; set; }
        public ECPublicKeyParameters PublicKey { get; set; }
        public X9ECParameters EcParameters { get; set; }

        public AnonymousTokenValidationKey AsValidationKey()
        {
            return new AnonymousTokenValidationKey(Kid, CurveName, EcParameters, PublicKey);
        }
    }
}
