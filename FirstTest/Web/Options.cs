using System.ComponentModel.DataAnnotations;

namespace Web;

public class Options
{
	public const string Section = "Options";

	[Required]
	public required string BasePath { get; set; }
}
