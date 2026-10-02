using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Workflows;

namespace NexusServiceMarketingSystem.Services.Workflows;

public interface ICustomerAccountService
{
    Task<(Customer Customer, User User)> CreateAsync(CustomerRegistrationViewModel model, CancellationToken cancellationToken = default);
}
