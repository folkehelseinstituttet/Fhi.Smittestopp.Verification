using System.Collections.Generic;

namespace Fhi.Smittestopp.Verification.Domain.Dtos
{
    public class AnonymousTokenKeySet
    {
        public ICollection<AnonymousTokenKey> Keys { get; set; }
    }

    public class AnonymousTokenKey
    {
        public string Kid { get; set; }
        public string Kty { get; set; }
        public string Crv { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string K { get; set; }
        public string PublicKeyAsHex { get; set; }
    }
}
