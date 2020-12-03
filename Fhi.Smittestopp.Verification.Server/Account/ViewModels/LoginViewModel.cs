using System.Collections.Generic;
using System.Linq;
using Fhi.Smittestopp.Verification.Server.Account.Models;

namespace Fhi.Smittestopp.Verification.Server.Account.ViewModels
{
    public class LoginViewModel : LoginInputModel
    {
        private readonly LoginOptions _options;

        public bool EnableLocalLogin => _options.EnableLocalLogin;
        public IEnumerable<ExternalProvider> VisibleExternalProviders => _options.ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));

        public LoginViewModel(string returnUrl, LoginOptions options)
        {
            ReturnUrl = returnUrl;
            _options = options;
        }
    }
}