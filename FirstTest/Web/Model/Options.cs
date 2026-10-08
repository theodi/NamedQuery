using System.ComponentModel.DataAnnotations;

namespace Web.Model;

public class Options
{
    public const string SectionName = "Options";

    [Required]
    public required Uri SparqlEndpoint { get; set; }
}
