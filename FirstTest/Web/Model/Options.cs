using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.FileProviders;

namespace Web.Model;

public class Options
{
    public const string SectionName = "Options";

    [Required]
    public required Uri SparqlEndpoint { get; set; }

    [Required]
    public required IFileProvider Definitions { get; set; }
}
