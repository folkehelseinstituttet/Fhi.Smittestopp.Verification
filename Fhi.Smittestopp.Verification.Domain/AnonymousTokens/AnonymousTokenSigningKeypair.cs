﻿using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class AnonymousTokenSigningKeypair
    {
        public AnonymousTokenSigningKeypair(string kid,
            string curveName,
            BigInteger privateKey,
            ECPoint publicKey)
        {
            Kid = kid;
            CurveName = curveName;
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }

        public string Kid { get; set; }
        public string CurveName { get; set; }
        public BigInteger PrivateKey { get; set; }
        public ECPoint PublicKey { get; set; }
        public X9ECParameters EcParameters => CustomNamedCurves.GetByName(CurveName);

        public AnonymousTokenValidationKey AsValidationKey()
        {
            return new AnonymousTokenValidationKey(Kid, CurveName, PublicKey);
        }
    }
}
