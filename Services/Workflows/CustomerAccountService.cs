using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;
using NexusServiceMarketingSystem.Models.Workflows;
using NexusServiceMarketingSystem.Services.Security;

namespace NexusServiceMarketingSystem.Services.Workflows;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;

    public CustomerAccountService(AppDbContext db, IPasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<(Customer Customer, User User)> CreateAsync(CustomerRegistrationViewModel model, CancellationToken cancellationToken = default)
    {
        string username = model.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(model.Password))
            throw new InvalidOperationException("A username and password are required.");
        if (model.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(model.CompanyName))
            throw new InvalidOperationException("Company name is required for a corporate customer.");
        if (!await _db.Cities.AnyAsync(c => c.Id == model.CityId && c.IsActive, cancellationToken))
            throw new InvalidOperationException("Select an active city.");
        if (await _db.Users.AnyAsync(u => u.Username == username, cancellationToken))
            throw new InvalidOperationException("That username is already in use.");
        if (!string.IsNullOrWhiteSpace(model.Email) && await _db.Customers.AnyAsync(c => c.Email == model.Email.Trim(), cancellationToken))
            throw new InvalidOperationException("A customer account with this email already exists. Contact the retail shop to update the existing profile.");

        var customer = new Customer
        {
            FullName = model.FullName.Trim(),
            CustomerType = model.CustomerType,
            CompanyName = model.CustomerType == CustomerType.Corporate ? model.CompanyName?.Trim() : null,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            Phone = model.Phone.Trim(),
            AddressLine = model.AddressLine.Trim(),
            PostalCode = model.PostalCode?.Trim(),
            CityId = model.CityId,
            IsActive = true
        };
        var user = new User { Username = username, IsActive = true, Customer = customer };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        _db.Customers.Add(customer);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return (customer, user);
    }
}
