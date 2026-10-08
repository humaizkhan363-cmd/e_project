using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

public class CustomerRegistrationViewModel : CustomerFormViewModel
{
    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
