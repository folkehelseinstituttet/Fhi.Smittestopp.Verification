using System;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Fhi.Smittestopp.Verification.Domain.Dtos
{
    /// <summary>
    /// Response object containing a successfully generated anonymous token
    /// </summary>
    public class AnonymousTokenResponse
    {
        /// <summary>
        /// The key-ID for the key pair used to generate the token
        /// </summary>
        public string Kid { get; set; }

        /// <summary>
        /// Base 64 encoded signed point (Q)
        /// </summary>
        public string SignedPoint { get; set; }

        /// <summary>
        /// Base 64 encoded proof challenge (c)
        /// </summary>
        public string ProofChallenge { get; set; }

        /// <summary>
        /// Base 64 encoded proof response (z)
        /// </summary>
        public string ProofResponse { get; set; }

        public AnonymousTokenResponse()
        {

        }

        public AnonymousTokenResponse(string kid, ECPoint signedPoint, BigInteger proofChallenge, BigInteger proofResponse)
        {
            Kid = kid;
            SignedPoint = Convert.ToBase64String(signedPoint.GetEncoded());
            ProofChallenge = Convert.ToBase64String(proofChallenge.ToByteArray());
            ProofResponse = Convert.ToBase64String(proofResponse.ToByteArray());
        }
    }
}
