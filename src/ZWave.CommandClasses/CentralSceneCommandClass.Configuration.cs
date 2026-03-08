using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Central Scene Configuration Report received from a device.
/// </summary>
public readonly record struct CentralSceneConfigurationReport(
    /// <summary>
    /// Whether the Slow Refresh capability is enabled.
    /// When <see langword="true"/>, the device sends Key Held Down refreshes every 55 seconds instead of 200ms.
    /// </summary>
    bool SlowRefresh);

public sealed partial class CentralSceneCommandClass
{
    /// <summary>
    /// Gets the last configuration report received from the device.
    /// </summary>
    public CentralSceneConfigurationReport? LastConfiguration { get; private set; }

    /// <summary>
    /// Event raised when a Central Scene Configuration Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<CentralSceneConfigurationReport>? OnConfigurationReportReceived;

    /// <summary>
    /// Request the configuration of optional node capabilities for scene notifications.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration report.</returns>
    public async Task<CentralSceneConfigurationReport> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        CentralSceneConfigurationGetCommand command = CentralSceneConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<CentralSceneConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        CentralSceneConfigurationReport report = CentralSceneConfigurationReportCommand.Parse(reportFrame, Logger);
        LastConfiguration = report;
        OnConfigurationReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Configure the use of optional node capabilities for scene notifications.
    /// </summary>
    /// <param name="slowRefresh">
    /// <see langword="true"/> to enable Slow Refresh (refreshes every 55 seconds).
    /// <see langword="false"/> to disable Slow Refresh (refreshes every 200ms).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetConfigurationAsync(bool slowRefresh, CancellationToken cancellationToken)
    {
        var command = CentralSceneConfigurationSetCommand.Create(slowRefresh);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct CentralSceneConfigurationSetCommand : ICommand
    {
        public CentralSceneConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneConfigurationSetCommand Create(bool slowRefresh)
        {
            // Configuration Set: Properties1 (1 byte)
            //   Bit 7: Slow Refresh
            //   Bits 6-0: Reserved (set to 0)
            ReadOnlySpan<byte> commandParameters = [(byte)(slowRefresh ? 0b1000_0000 : 0)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new CentralSceneConfigurationSetCommand(frame);
        }
    }

    internal readonly struct CentralSceneConfigurationGetCommand : ICommand
    {
        public CentralSceneConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new CentralSceneConfigurationGetCommand(frame);
        }
    }

    internal readonly struct CentralSceneConfigurationReportCommand : ICommand
    {
        public CentralSceneConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        public static CentralSceneConfigurationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Configuration Report: Properties1 (1 byte)
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Central Scene Configuration Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Central Scene Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Properties1: Bit 7 = Slow Refresh, Bits 6-0 = Reserved
            bool slowRefresh = (span[0] & 0b1000_0000) != 0;

            return new CentralSceneConfigurationReport(slowRefresh);
        }
    }
}
