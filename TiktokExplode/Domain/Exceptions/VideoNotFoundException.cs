namespace TiktokExplode.Domain.Exceptions;

public sealed class VideoNotFoundException : TiktokException
{
    public VideoNotFoundException()
        : base("The specified video could not be found.")
    {
    }

    public VideoNotFoundException(string message) : base(message)
    {
    }

    public VideoNotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}