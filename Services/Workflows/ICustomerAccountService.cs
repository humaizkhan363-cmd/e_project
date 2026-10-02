using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Services.Workflows;

/// <summary>Creates a customer together with their login account (used by self-registration, retail staff and Admin).</summary>
public interface ICustomerAccountService
{
    Task<(Customer Customer, User User)> CreateAsync(CustomerRegistrationViewModel model, CancellationToken cancellationToken = default);
}
