namespace ZWave.Serial.Commands;

public interface IRequestWithCallback<TCommand> : ICommand<TCommand>
    where TCommand : struct, ICommand<TCommand>
{
    /// <summary>
    /// Indicates whether this request expects a <see cref="ResponseStatus"/> response.
    /// </summary>
    public static abstract bool ExpectsResponseStatus { get; }

    public byte SessionId { get; }
}