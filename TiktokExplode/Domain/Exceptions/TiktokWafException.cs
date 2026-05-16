namespace TiktokExplode.Domain.Exceptions;

public sealed class TiktokWafException : TiktokException
{
    public TiktokWafException()
        : base("The request was blocked by TikTok's Web Application Firewall (WAF).")
    {
    }

    public TiktokWafException(string message) : base(message)
    {
    }

    public TiktokWafException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}