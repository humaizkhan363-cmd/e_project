using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models.Account;

public sealed class ChangePasswordViewModel
{
    [Required,DataType(DataType.Password),Display(Name="Current password")] public string CurrentPassword{get;set;}=string.Empty;
    [Required,MinLength(10),DataType(DataType.Password),Display(Name="New password")] public string NewPassword{get;set;}=string.Empty;
    [Required,DataType(DataType.Password),Compare(nameof(NewPassword)),Display(Name="Confirm new password")] public string ConfirmPassword{get;set;}=string.Empty;
}
