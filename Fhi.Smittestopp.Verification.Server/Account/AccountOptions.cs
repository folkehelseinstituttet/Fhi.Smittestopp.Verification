namespace Fhi.Smittestopp.Verification.Server.Account
{
    public class AccountOptions
    {
        public static bool AllowLocalLogin = true;

        public static bool ShowLogoutPrompt = true;
        public static bool AutomaticRedirectAfterSignOut = false;

        public static string InvalidCredentialsErrorMessage = "Invalid PIN-code";
    }
}
