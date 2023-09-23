using System.Runtime.Serialization;

namespace Squidlr.Client;

[Serializable]
public class GuestTokenException : Exception
{
    public GuestTokenException() { }

    public GuestTokenException(string message) : base(message) { }

    public GuestTokenException(string message, Exception inner) : base(message, inner) { }

    protected GuestTokenException(
      SerializationInfo info,
      StreamingContext context) : base(info, context) { }
}
