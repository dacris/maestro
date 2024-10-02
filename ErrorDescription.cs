using Newtonsoft.Json;

namespace Dacris.Maestro;

public class ErrorDescription
{
    [JsonProperty("message")]
    public string Message { get; set; }
    [JsonProperty("innerException")]
    public string? InnerException { get; set; }
    [JsonProperty("stackTrace")]
    public string? StackTrace { get; set; }
    [JsonProperty("whichInteraction")]
    public string? WhichInteraction { get; set; }
    [JsonProperty("timeUtc")]
    public DateTime TimeUtc { get; set; }

    public ErrorDescription(string message, Exception? innerException, string? stackTrace, string? whichInteraction, DateTime timeUtc)
    {
        Message = message;
        InnerException = innerException?.Message;
        StackTrace = stackTrace;
        WhichInteraction = whichInteraction;
        TimeUtc = timeUtc;
    }

    public override bool Equals(object? obj)
    {
        return obj is ErrorDescription other &&
               Message == other.Message &&
               InnerException == other.InnerException &&
               StackTrace == other.StackTrace &&
               WhichInteraction == other.WhichInteraction &&
               TimeUtc == other.TimeUtc;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, InnerException, StackTrace, WhichInteraction, TimeUtc);
    }
}