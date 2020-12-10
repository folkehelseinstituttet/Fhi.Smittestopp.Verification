namespace Fhi.Smittestopp.Verification.Server
{
    public class InteractionConfig
    {
        /// <summary>
        /// If false, disables the home page
        /// </summary>
        public bool EnableHomePage { get; set; }

        /// <summary>
        /// If true, /account/login is only available when a valid authorization request is found
        /// </summary>
        public bool RequireAuthorizationRequest { get; set; }

        /// <summary>
        /// If true, displays error description in the error page
        /// </summary>
        public bool DisplayErrorDescription { get; set; }

        /// <summary>
        /// If true, uses redirect with better UX for native clients for non http(s) return urls.
        /// </summary>
        public bool UseNativeRedirect { get; set; }
    }
}
