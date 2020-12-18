using System;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenValidationKey
    {
        public AnonymousTokenValidationKey(string kid, string curveName, X9ECParameters ecParameters, ECPublicKeyParameters publicKey)
        {
            Kid = kid;
            CurveName = curveName;
            EcParameters = ecParameters;
            PublicKey = publicKey;
        }

        public string Kid { get; set; }
        public string KeyType => "EC";
        public string CurveName { get; set; }
        public ECPublicKeyParameters PublicKey { get; set; }
        public X9ECParameters EcParameters { get; set; }

        public string GetEncodedKey()
        {
            return Hex.ToHexString(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(PublicKey).GetEncoded());
        }

        public AnonymousTokenKey AsKeyDto()
        {
            return new AnonymousTokenKey
            {
                Kid = Kid,
                Kty = KeyType,
                Crv = CurveName,
                X = Convert.ToBase64String(PublicKey.Q.AffineXCoord.ToBigInteger().ToByteArray()),
                Y = Convert.ToBase64String(PublicKey.Q.AffineYCoord.ToBigInteger().ToByteArray())
            };
        }
    }
}
