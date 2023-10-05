namespace Squidlr.Twitter;

public enum GetTweetVideoResult
{
    /// <summary>
    /// The Tweet video was requested successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The Tweet was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The Tweet contains no video.
    /// </summary>
    NoVideo,

    /// <summary>
    /// The Tweet contains an embedded video source which is not yet supported.
    /// </summary>
    UnsupportedVideo,

    /// <summary>
    /// The account containing the requested Tweet has been suspended.
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
    /// The Twitter backend could not be accessed successfully.
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
