using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MeterCommandClass
{
    /// <summary>
    /// Resets the accumulated meter value of the given type at the device to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// Only Meter command class version 6 supports a targeted reset. Earlier versions can only
    /// reset all accumulated values to zero; use <see cref="ResetAllAsync(CancellationToken)"/> for that.
    /// The <paramref name="scale"/> is validated against <paramref name="type"/> but not transmitted:
    /// per the specification the device applies the scale it used in its last Meter Report, which
    /// cannot be selected or guaranteed. The <paramref name="rateType"/> is not transmitted either.
    /// </remarks>
    /// <param name="type">The meter type whose accumulated value is reset.</param>
    /// <param name="scale">The scale of the value being reset. Validated but not transmitted.</param>
    /// <param name="rateType">
    /// Accepted for API symmetry with <see cref="GetAsync(MeterType?, MeterScale?, MeterRateType?, CancellationToken)"/>.
    /// Not transmitted; the reset wire format carries no rate type.
    /// </param>
    /// <param name="value">The accumulated value to reset to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ZWaveException">
    /// <see cref="ZWaveErrorCode.CommandInvalidArgument"/> if the scale is not valid for the meter type;
    /// <see cref="ZWaveErrorCode.CommandNotReady"/> if whether the device supports Reset has not yet
    /// been discovered;
    /// <see cref="ZWaveErrorCode.CommandNotSupported"/> if the negotiated version is below 6 or the device
    /// does not advertise Reset support.
    /// </exception>
    public async Task ResetAsync(
        MeterType type,
        MeterScale scale,
        MeterRateType rateType,
        int value,
        CancellationToken cancellationToken)
    {
        if (EffectiveVersion < 6)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandNotSupported,
                "A targeted Meter Reset requires command class version 6; use ResetAllAsync to reset all accumulated values to zero.");
        }

        RequireResetSupportDiscovered();

        if (!MeterScaleHelper.TryGetEncoding(type, scale, out _, out _))
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, $"Scale '{scale}' is not a valid scale for meter type '{type}'.");
        }

        // The rate type is not part of the reset wire format; it is accepted for API symmetry only.
        _ = rateType;

        var command = MeterResetCommand.Create(EffectiveVersion, type, value);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resets all accumulated meter values at the device to zero.
    /// </summary>
    /// <remarks>
    /// This is the legacy (version 2-5) reset, which has no parameters. Version 6 has no
    /// parameterless reset; use <see cref="ResetAsync(MeterType, MeterScale, MeterRateType, int, CancellationToken)"/>
    /// with a value of zero for the device's meter type instead.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ZWaveException">
    /// <see cref="ZWaveErrorCode.CommandNotReady"/> if whether the device supports Reset has not yet
    /// been discovered;
    /// <see cref="ZWaveErrorCode.CommandNotSupported"/> if the negotiated version is below 2 or is 6, or the
    /// device does not advertise Reset support.
    /// </exception>
    public async Task ResetAllAsync(CancellationToken cancellationToken)
    {
        if (EffectiveVersion is < 2 or >= 6)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandNotSupported,
                EffectiveVersion >= 6
                    ? "Version 6 has no parameterless reset; use ResetAsync with a value of zero instead."
                    : "This node does not support the Meter Reset command.");
        }

        RequireResetSupportDiscovered();

        var command = MeterResetCommand.CreateResetAll();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private void RequireResetSupportDiscovered()
    {
        if (!ResetSupported.HasValue)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandNotReady,
                "Whether the device supports the Meter Reset command is not yet known; interview the node first.");
        }

        if (ResetSupported is false)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandNotSupported,
                "The device does not support the Meter Reset command.");
        }
    }

    internal readonly struct MeterResetCommand : ICommand
    {
        public MeterResetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Meter;

        public static byte CommandId => (byte)MeterCommand.Reset;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Creates the V6 targeted reset: Size (3 bits) | Meter Type (5 bits), followed by the signed
        /// Meter Value (1, 2, or 4 bytes).
        /// </summary>
        public static MeterResetCommand Create(byte version, MeterType meterType, int value)
        {
            if (version < 6)
            {
                ZWaveException.Throw(
                    ZWaveErrorCode.CommandInvalidArgument,
                    $"A targeted Meter Reset is only available at version 6; the negotiated version is {version}.");
            }

            int size = value.GetSignedVariableSize();
            byte byte0 = (byte)(((size & 0b0000_0111) << 5) | ((byte)meterType & 0b0001_1111));

            Span<byte> parameters = stackalloc byte[1 + size];
            parameters[0] = byte0;
            value.WriteSignedVariableSizeBE(parameters.Slice(1, size));

            return new MeterResetCommand(CommandClassFrame.Create(CommandClassId, CommandId, parameters));
        }

        /// <summary>
        /// Creates the legacy V2-5 reset, which has no parameters and resets all accumulated
        /// values to zero.
        /// </summary>
        public static MeterResetCommand CreateResetAll()
            => new(CommandClassFrame.Create(CommandClassId, CommandId));
    }
}
