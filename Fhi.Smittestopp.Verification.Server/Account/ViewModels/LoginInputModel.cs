using System.ComponentModel.DataAnnotations;

namespace Fhi.Smittestopp.Verification.Server.Account.ViewModels
{
    public class LoginInputModel
    {
        [Required]
        public string PinCode { get; set; }
        public string ReturnUrl { get; set; }
    }
}