﻿namespace Squidlr.Web.States;

public sealed class AppState
{
    public string? UserId => SessionId;

    public string? SessionId { get; set; }

    public string? Referer { get; set; }

    public string? UserAgent { get; set; }

    public bool DoNotTrack { get; set; }
}
