using System;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Org.BouncyCastle.Math.EC;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenValidationKey
    {
        public AnonymousTokenValidationKey(string kid, string curveName, ECPoint publicKey)
        {
            Kid = kid;
            CurveName = curveName;
            PublicKey = publicKey;
        }

        public string Kid { get; set; }
        public string KeyType => "EC";
        public string CurveName { get; set; }
        public ECPoint PublicKey { get; set; }

        public AnonymousTokenKey AsKeyDto()
        {
            return new AnonymousTokenKey
            {
                Kid = Kid,
                Kty = KeyType,
                Crv = CurveName,
                X = Convert.ToBase64String(PublicKey.AffineXCoord.ToBigInteger().ToByteArray()),
                Y = Convert.ToBase64String(PublicKey.AffineYCoord.ToBigInteger().ToByteArray())
            };
        }
    }
}
