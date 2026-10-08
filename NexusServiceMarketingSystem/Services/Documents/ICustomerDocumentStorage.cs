namespace NexusServiceMarketingSystem.Services.Documents;

/// <summary>Saves, locates and deletes customer document files (PDF / PNG / JPEG, max 5 MB).</summary>
public interface ICustomerDocumentStorage
{
    /// <summary>Validates and saves the upload; returns the generated storage name, content type and size.</summary>
    Task<(string StorageName, string ContentType, long SizeBytes)> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Full path of a stored document.</summary>
    string GetPath(string storageName);

    /// <summary>Deletes a stored document if it exists.</summary>
    void Delete(string storageName);
}
