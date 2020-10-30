using System.Collections.Generic;
using System.Linq;

namespace Fhi.Smittestopp.Verification.Server.Account.Models
{
    public class LoginOptions
    {
        public bool EnableLocalLogin { get; set; } = true;
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProvider>();
        public string LoginHint { get; set; }


        public bool IsExternalLoginOnly => !EnableLocalLogin && ExternalProviders?.Count() == 1;
        public string ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
    }
}
