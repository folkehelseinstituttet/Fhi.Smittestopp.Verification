using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;

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

        public string GetEncodedKey()
        {
            return Hex.ToHexString(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(PublicKey).GetEncoded());
        }
    }
}
