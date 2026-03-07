namespace TheLsmArchive.ApiClient;

/// <summary>
/// The result of an operation.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract record Result<T>
{
    private Result() { }

    public sealed record Success(T Data) : Result<T>;

    public sealed record NoContent : Result<T>;

    public sealed record Failure(string Message) : Result<T>;

    /// <summary>
    /// Returns a successful result with the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns>The successful result.</returns>
    public static Result<T> Ok(T data) => new Success(data);

    /// <summary>
    /// Returns a no content result.
    /// </summary>
    /// <returns></returns>
    public static Result<T> None() => new NoContent();

    /// <summary>
    /// Returns a failure result with the specified message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>The failure result.</returns>
    public static Result<T> Fail(string message) => new Failure(message);
}
