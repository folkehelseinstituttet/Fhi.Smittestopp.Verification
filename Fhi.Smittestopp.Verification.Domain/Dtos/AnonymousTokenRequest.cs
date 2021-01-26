namespace Fhi.Smittestopp.Verification.Domain.Dtos
{
    /// <summary>
    /// Request object to generate an anonymous token
    /// </summary>
    public class AnonymousTokenRequest
    {
        /// <summary>
        /// Base 64 encoded masked point (P) to be signed as an anonymous token
        /// </summary>
        public string MaskedPoint { get; set; }
    }
}
