using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Squidlr;

public sealed class TweetCrawlerOptions : IOptions<TweetCrawlerOptions>, IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public string? AuthorizationBearerToken { get; set; }

    [Required]
    public Uri? TwitterApiHostUri { get; set; }

    public TweetCrawlerOptions Value => this;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break;
    }
}
