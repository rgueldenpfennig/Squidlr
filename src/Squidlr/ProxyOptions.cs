using System.ComponentModel.DataAnnotations;

namespace Squidlr;

public sealed class ProxyOptions : IValidatableObject
{
    public Uri? ProxyAddress { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool UseProxy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UseProxy && ProxyAddress == null)
        {
            yield return new ValidationResult(
                "Proxy usage is activated, so a proxy address must be provided.",
                new string[] { nameof(ProxyAddress) });
        }

        yield break;
    }
}
