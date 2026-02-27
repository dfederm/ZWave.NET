using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class ColorSwitchCommandClass
{
    /// <summary>
    /// Gets the color components supported by the device.
    /// </summary>
    public IReadOnlySet<ColorSwitchColorComponent>? SupportedComponents { get; private set; }

    /// <summary>
    /// Request the supported color components of a device.
    /// </summary>
    public async Task<IReadOnlySet<ColorSwitchColorComponent>> GetSupportedAsync(CancellationToken cancellationToken)
    {
        ColorSwitchSupportedGetCommand command = ColorSwitchSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ColorSwitchSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<ColorSwitchColorComponent> supportedComponents = ColorSwitchSupportedReportCommand.Parse(reportFrame, Logger);

        ApplySupportedComponents(supportedComponents);

        return supportedComponents;
    }

    private void ApplySupportedComponents(IReadOnlySet<ColorSwitchColorComponent> supportedComponents)
    {
        SupportedComponents = supportedComponents;

        Dictionary<ColorSwitchColorComponent, ColorSwitchReport?> newColorComponents = new Dictionary<ColorSwitchColorComponent, ColorSwitchReport?>();
        foreach (ColorSwitchColorComponent colorComponent in supportedComponents)
        {
            // Persist any existing known state.
            ColorSwitchReport? colorComponentState = null;
            if (_colorComponents != null
                && _colorComponents.TryGetValue(colorComponent, out ColorSwitchReport? existingState))
            {
                colorComponentState = existingState;
            }

            newColorComponents.Add(colorComponent, colorComponentState);
        }

        _colorComponents = newColorComponents;
    }

    internal readonly struct ColorSwitchSupportedGetCommand : ICommand
    {
        public ColorSwitchSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ColorSwitchSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ColorSwitchSupportedGetCommand(frame);
        }
    }

    internal readonly struct ColorSwitchSupportedReportCommand : ICommand
    {
        public ColorSwitchSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ColorSwitch;

        public static byte CommandId => (byte)ColorSwitchCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static IReadOnlySet<ColorSwitchColorComponent> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Color Switch Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Color Switch Supported Report frame is too short");
            }

            HashSet<ColorSwitchColorComponent> supportedComponents = [];

            ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span;
            for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        ColorSwitchColorComponent colorComponent = (ColorSwitchColorComponent)((byteNum << 3) + bitNum);
                        supportedComponents.Add(colorComponent);
                    }
                }
            }

            return supportedComponents;
        }
    }
}
