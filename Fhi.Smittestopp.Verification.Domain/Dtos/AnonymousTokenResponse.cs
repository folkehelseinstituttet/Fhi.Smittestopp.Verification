using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Utilities.Encoders;

namespace Fhi.Smittestopp.Verification.Domain.Dtos
{
    public class AnonymousTokenResponse
    {
        public string QAsHex { get; set; }

        public string ProofCAsHex { get; set; }

        public string ProofZAsHex { get; set; }

        public AnonymousTokenResponse()
        {

        }

        public AnonymousTokenResponse(ECPoint Q, BigInteger proofC, BigInteger proofZ)
        {
            QAsHex = Hex.ToHexString(Q.GetEncoded());
            ProofCAsHex = Hex.ToHexString(proofC.ToByteArray());
            ProofZAsHex = Hex.ToHexString(proofZ.ToByteArray());
        }
    }
}
