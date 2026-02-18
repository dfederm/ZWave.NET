namespace ZWave.Serial.Commands;

/// <summary>
/// Defines a Serial API request command that expects a callback response.
/// </summary>
public interface IRequestWithCallback<TCommand> : ICommand<TCommand>
    where TCommand : struct, ICommand<TCommand>
{
    /// <summary>
    /// Indicates whether this request expects a <see cref="ResponseStatus"/> response.
    /// </summary>
    public static abstract bool ExpectsResponseStatus { get; }

    /// <summary>
    /// Gets the session ID used to correlate this request with its callback.
    /// </summary>
    public byte SessionId { get; }
}