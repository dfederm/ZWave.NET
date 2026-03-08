using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class WindowCoveringCommandClass
{
    /// <summary>
    /// Gets the parameter IDs supported by the device.
    /// </summary>
    public IReadOnlySet<WindowCoveringParameterId>? SupportedParameterIds { get; private set; }

    /// <summary>
    /// Request the supported properties of a device.
    /// </summary>
    public async Task<IReadOnlySet<WindowCoveringParameterId>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        WindowCoveringSupportedGetCommand command = WindowCoveringSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<WindowCoveringSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<WindowCoveringParameterId> supportedParameters = WindowCoveringSupportedReportCommand.Parse(reportFrame, Logger);

        ApplySupportedParameters(supportedParameters);

        return supportedParameters;
    }

    private void ApplySupportedParameters(IReadOnlySet<WindowCoveringParameterId> supportedParameters)
    {
        SupportedParameterIds = supportedParameters;

        Dictionary<WindowCoveringParameterId, WindowCoveringReport?> newParameterValues = new Dictionary<WindowCoveringParameterId, WindowCoveringReport?>();
        foreach (WindowCoveringParameterId parameterId in supportedParameters)
        {
            // Persist any existing known state.
            WindowCoveringReport? parameterState = null;
            if (_parameterValues.TryGetValue(parameterId, out WindowCoveringReport? existingState))
            {
                parameterState = existingState;
            }

            newParameterValues.Add(parameterId, parameterState);
        }

        _parameterValues = newParameterValues;
    }

    internal readonly struct WindowCoveringSupportedGetCommand : ICommand
    {
        public WindowCoveringSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static WindowCoveringSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new WindowCoveringSupportedGetCommand(frame);
        }
    }

    internal readonly struct WindowCoveringSupportedReportCommand : ICommand
    {
        public WindowCoveringSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.WindowCovering;

        public static byte CommandId => (byte)WindowCoveringCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<WindowCoveringParameterId> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Window Covering Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Window Covering Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            int maskByteCount = span[0] & 0b0000_1111;
            if (maskByteCount < 1 || frame.CommandParameters.Length < 1 + maskByteCount)
            {
                logger.LogWarning(
                    "Window Covering Supported Report has invalid mask byte count ({MaskByteCount}) for frame length ({Length} bytes)",
                    maskByteCount,
                    frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Window Covering Supported Report has invalid mask byte count");
            }

            ReadOnlySpan<byte> maskBytes = span.Slice(1, maskByteCount);
            return BitMaskHelper.ParseBitMask<WindowCoveringParameterId>(maskBytes);
        }
    }
}
