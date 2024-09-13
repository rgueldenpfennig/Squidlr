using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Squidlr;

public sealed class SquidlrOptions : IOptions<SquidlrOptions>, IValidatableObject
{
    [Required]
    public Uri? InstagramHostUri { get; set; }

    [Required]
    public Uri? TiktokHostUri { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? TwitterAuthorizationBearerToken { get; set; }

    [Required]
    public Uri? TwitterApiHostUri { get; set; }

    [Required]
    public Uri? LinkedInHostUri { get; set; }

    public ProxyOptions? ProxyOptions { get; set; }

    public SquidlrOptions Value => this;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break;
    }
}
