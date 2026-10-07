using System.ComponentModel.DataAnnotations;

namespace Web;

public class Options
{
    public const string SectionName = "Options";

    [Required]
    public required Uri SparqlEndpoint { get; set; }
}
