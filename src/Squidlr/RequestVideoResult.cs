namespace Squidlr;

public enum RequestVideoResult
{
    /// <summary>
    /// The video was requested successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The content was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The content contains no video.
    /// </summary>
    NoVideo,

    /// <summary>
    /// The content contains an embedded video source which is not yet supported.
    /// </summary>
    UnsupportedVideo,

    /// <summary>
    /// The account containing the requested content has been suspended.
    /// </summary>
    AccountSuspended,

    /// <summary>
    /// The account owner limits who can view their Posts.
    /// </summary>
    Protected,

    /// <summary>
    /// Age-restricted adult content. This content might not be appropriate for people under 18 years old.
    /// </summary>
    AdultContent,

    /// <summary>
    /// The backend could not be accessed successfully.
    /// </summary>
    GatewayError,

    /// <summary>
    /// The process has been cancelled.
    /// </summary>
    Canceled,

    /// <summary>
    /// An unspecified error occurred.
    /// </summary>
    Error
}
