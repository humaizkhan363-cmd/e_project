using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models.Account
{
    /// <summary>Form model for the login page. Carries no identifiers, no roles \u2014 those are looked up server-side.</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }

        /// <summary>Where to return to after a successful login (set from the "Access Denied" redirect).</summary>
        public string? ReturnUrl { get; set; }
    }
}
