using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Models.Workflows;

public class CustomerDocumentFormViewModel
{
    public int CustomerId { get; set; }
    [Required] public CustomerDocumentType? DocumentType { get; set; }
    [Range(2000,2100)] public int DocumentYear { get; set; }=DateTime.Today.Year;
    [StringLength(500)] public string? Notes { get; set; }
    [Required] public IFormFile? File { get; set; }
    public string CustomerName { get; set; }=string.Empty;
    public List<SelectListItem> Documents { get; set; }=new();
}
