namespace NexusServiceMarketingSystem.Services.Documents;

public sealed class CustomerDocumentStorage(IWebHostEnvironment environment):ICustomerDocumentStorage
{
    private const long MaxBytes=5*1024*1024;
    private static readonly IReadOnlyDictionary<string,(string Mime,byte[] Signature)> Types=new Dictionary<string,(string,byte[])>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]=("application/pdf",new byte[]{0x25,0x50,0x44,0x46}),
        [".png"]=("image/png",new byte[]{0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a}),
        [".jpg"]=("image/jpeg",new byte[]{0xff,0xd8,0xff}),
        [".jpeg"]=("image/jpeg",new byte[]{0xff,0xd8,0xff})
    };
    private string Root=>Path.Combine(environment.ContentRootPath,"App_Data","customer-documents");
    public async Task<(string StorageName,string ContentType,long SizeBytes)> SaveAsync(IFormFile file,CancellationToken cancellationToken=default)
    {
        if(file is null||file.Length==0||file.Length>MaxBytes)throw new InvalidOperationException("Choose a non-empty PDF, PNG, or JPEG file no larger than 5 MB.");
        string ext=Path.GetExtension(Path.GetFileName(file.FileName));if(!Types.TryGetValue(ext,out var type))throw new InvalidOperationException("Only PDF, PNG, and JPEG files are accepted.");
        await using var input=file.OpenReadStream();byte[] header=new byte[type.Signature.Length];int read=await input.ReadAsync(header.AsMemory(0,header.Length),cancellationToken);
        if(read!=header.Length||!header.SequenceEqual(type.Signature))throw new InvalidOperationException("The uploaded file content does not match its file type.");
        string name=Guid.NewGuid().ToString("N")+ext.ToLowerInvariant();Directory.CreateDirectory(Root);string path=GetPath(name);
        await using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous|FileOptions.SequentialScan);
        await input.CopyToAsync(output,cancellationToken);return(name,type.Mime,file.Length);
    }
    public string GetPath(string storageName)
    {
        if(storageName.Length is < 36 or > 45||storageName.IndexOfAny(Path.GetInvalidFileNameChars())>=0||storageName.Contains("..",StringComparison.Ordinal))throw new InvalidOperationException("Invalid document reference.");
        return Path.Combine(Root,storageName);
    }
    public void Delete(string storageName){var path=GetPath(storageName);if(File.Exists(path))File.Delete(path);}
}
