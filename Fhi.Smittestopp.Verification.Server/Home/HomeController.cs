using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Server.Home
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ILogger _logger;
        private readonly IOptions<InteractionConfig> _interactionConfig;

        public HomeController(IIdentityServerInteractionService interaction, ILogger<HomeController> logger, IOptions<InteractionConfig> interactionConfig)
        {
            _interaction = interaction;
            _logger = logger;
            _interactionConfig = interactionConfig;
        }

        public IActionResult Index()
        {
            if (_interactionConfig.Value.EnableHomePage)
            {
                // only show in development
                return View();
            }

            _logger.LogInformation("Homepage is disabled for this environment. Returning 404.");
            return NotFound();
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel(await _interaction.GetErrorContextAsync(errorId), _interactionConfig.Value.DisplayErrorDescription);

            return View("Error", vm);
        }
    }
}