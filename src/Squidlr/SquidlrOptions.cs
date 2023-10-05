using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Squidlr;

public sealed class SquidlrOptions : IOptions<SquidlrOptions>, IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public string? AuthorizationBearerToken { get; set; }

    [Required]
    public Uri? TwitterApiHostUri { get; set; }

    public SquidlrOptions Value => this;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break;
    }
}
