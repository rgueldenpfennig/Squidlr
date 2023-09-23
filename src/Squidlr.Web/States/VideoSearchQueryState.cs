using System.ComponentModel.DataAnnotations;

namespace Squidlr.Web.States;

public sealed class VideoSearchQueryState
{
    [Required(AllowEmptyStrings = false)]
    public string? Url { get; set; }
}
