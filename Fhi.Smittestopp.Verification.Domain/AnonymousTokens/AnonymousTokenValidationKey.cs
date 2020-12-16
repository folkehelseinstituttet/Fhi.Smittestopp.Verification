using Org.BouncyCastle.Crypto.Parameters;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenValidationKey
    {
        public AnonymousTokenValidationKey(string kid, ECPublicKeyParameters publicKey)
        {
            Kid = kid;
            PublicKey = publicKey;
        }

        public string Kid { get; set; }
        public ECPublicKeyParameters PublicKey { get; set; }
    }
}
