using System.ComponentModel.DataAnnotations;

namespace Squidlr.Web.States;

public sealed class RequestVideoState
{
    [Required(AllowEmptyStrings = false)]
    public string? TwitterUrl { get; set; }
}
