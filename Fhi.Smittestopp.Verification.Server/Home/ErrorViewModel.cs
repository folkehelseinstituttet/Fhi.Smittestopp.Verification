using IdentityServer4.Models;

namespace Fhi.Smittestopp.Verification.Server.Home
{
    public class ErrorViewModel
    {
        private readonly ErrorMessage _error;
        private readonly bool _displayErrorDescription;

        public ErrorViewModel(ErrorMessage error, bool displayErrorDescription)
        {
            _error = error;
            _displayErrorDescription = displayErrorDescription;
        }

        public string RequestId => _error?.RequestId;
        public string Error => _error?.Error;
        public string ErrorDescription => _displayErrorDescription ? _error?.ErrorDescription : null;
    }
}