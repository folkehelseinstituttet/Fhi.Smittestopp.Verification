using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenSigningKeypair
    {
        public AnonymousTokenSigningKeypair(string kid, BigInteger privateKey, ECPublicKeyParameters publicKey)
        {
            Kid = kid;
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }

        public string Kid { get; set; }
        public BigInteger PrivateKey { get; set; }
        public ECPublicKeyParameters PublicKey { get; set; }

        public AnonymousTokenValidationKey AsValidationKey()
        {
            return new AnonymousTokenValidationKey(Kid, PublicKey);
        }
    }
}
