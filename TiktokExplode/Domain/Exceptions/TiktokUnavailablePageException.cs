namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when TikTok answers with a well-formed response that carries no usable payload —
/// the branded placeholder shell (the "logo page"), a hydration blob with an empty
/// <c>itemStruct</c>, or a search response without a <c>data</c> array.
/// <para>
/// This is a soft block, not a structural change and not a missing item: TikTok knows the
/// content exists but declined to serve it for this particular request. It is
/// <see cref="TiktokException.IsTransient">transient</see> and worth retrying.
/// </para>
/// </summary>
public sealed class TiktokUnavailablePageException : TiktokException
{
    /// <summary>Always <see langword="true"/>: a soft block usually clears on a later attempt.</summary>
    public override bool IsTransient => true;

    /// <summary>Initializes a new instance with a default message.</summary>
    public TiktokUnavailablePageException()
        : base("TikTok returned a placeholder page without content. The request was most likely soft-blocked.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public TiktokUnavailablePageException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public TiktokUnavailablePageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
