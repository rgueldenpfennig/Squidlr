namespace Squidlr.Web.States;

public sealed class AppState
{
    public string SessionId { get; } = Guid.NewGuid().ToString();

    public string? Referer { get; set; }

    public string? UserAgent { get; set; }
}
