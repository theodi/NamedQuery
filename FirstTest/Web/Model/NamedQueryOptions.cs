using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.FileProviders;

namespace Web.Model;

public class NamedQueryOptions
{
    public const string SectionName = "NamedQuery";

    [Required]
    public required Uri SparqlEndpoint { get; set; }

    [Required]
    public required IFileProvider Definitions { get; set; }
}
