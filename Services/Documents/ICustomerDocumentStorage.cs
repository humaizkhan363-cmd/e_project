namespace NexusServiceMarketingSystem.Services.Documents;

public interface ICustomerDocumentStorage
{
    Task<(string StorageName,string ContentType,long SizeBytes)> SaveAsync(IFormFile file,CancellationToken cancellationToken=default);
    string GetPath(string storageName);
    void Delete(string storageName);
}
