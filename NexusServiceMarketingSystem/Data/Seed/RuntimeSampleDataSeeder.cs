using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Models.Entities;

namespace NexusServiceMarketingSystem.Data.Seed
{
    /// <summary>
    /// Adds a few starter cities (each with its unique 3-digit code) and one retail shop per city,
    /// so the "Select a city" dropdowns are not empty on a fresh database.
    /// Runs only when the Cities table is completely empty, so it never touches data the Admin
    /// has already entered. The Admin can edit or deactivate these from Master data.
    /// </summary>
    public static class RuntimeSampleDataSeeder
    {
        public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
        {
            if (await db.Cities.AnyAsync(cancellationToken))
            {
                return;
            }

            var cities = new[]
            {
                new City { Name = "Karachi",   Code = "001", IsActive = true },
                new City { Name = "Lahore",    Code = "002", IsActive = true },
                new City { Name = "Islamabad", Code = "003", IsActive = true },
                new City { Name = "Rawalpindi", Code = "004", IsActive = true },
                new City { Name = "Faisalabad", Code = "005", IsActive = true }
            };
            db.Cities.AddRange(cities);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var city in cities)
            {
                db.RetailShops.Add(new RetailShop
                {
                    Name = $"Nexus {city.Name} Main Shop",
                    AddressLine = $"Main Boulevard, {city.Name}",
                    Phone = "0000000000",
                    CityId = city.Id,
                    IsActive = true
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
