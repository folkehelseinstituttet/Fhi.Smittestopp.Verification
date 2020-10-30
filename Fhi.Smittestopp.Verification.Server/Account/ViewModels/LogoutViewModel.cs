namespace Fhi.Smittestopp.Verification.Server.Account.ViewModels
{
    public class LogoutViewModel : LogoutInputModel
    {
        public LogoutViewModel(string logoutId)
        {
            LogoutId = logoutId;
        }
    }
}
