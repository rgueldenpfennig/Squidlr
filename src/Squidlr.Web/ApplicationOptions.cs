using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Squidlr.Web;

public sealed class ApplicationOptions : IOptions<ApplicationOptions>, IValidatableObject
{
    public static string SessionCookieName { get; } = "X-Squidlr-Session";

    [Required]
    public string? Domain { get; set; }

    [Required]
    public string? InternalDomain { get; set; }

    [Required]
    public string? ApiKey { get; set; }

    [Required]
    public Uri? ApiHostUri { get; set; }

    private Uri? _apiHostUriHttps;

    public Uri? ApiHostUriHttps
    {
        get
        {
            if (_apiHostUriHttps != null)
                return _apiHostUriHttps;

            var uriBuilder = new UriBuilder(ApiHostUri!)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1 // default port for scheme
            };

            _apiHostUriHttps = uriBuilder.Uri;
            return _apiHostUriHttps;
        }
    }

    public ApplicationOptions Value => this;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break;
    }
}
