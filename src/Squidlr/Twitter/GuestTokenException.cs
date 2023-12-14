namespace Squidlr.Twitter;

[Serializable]
public class GuestTokenException : Exception
{
    public GuestTokenException() { }

    public GuestTokenException(string message) : base(message) { }

    public GuestTokenException(string message, Exception inner) : base(message, inner) { }
}
