using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Central Scene Supported Report received from a device.
/// </summary>
public readonly record struct CentralSceneSupportedReport(
    /// <summary>
    /// The maximum number of scenes supported by the device.
    /// Scenes are numbered in the range 1 to <see cref="SupportedScenes"/>.
    /// </summary>
    byte SupportedScenes,

    /// <summary>
    /// Whether the device supports the Slow Refresh capability (version 3).
    /// <see langword="null"/> if the payload does not include this field (version 1).
    /// </summary>
    bool? SlowRefreshSupport,

    /// <summary>
    /// Whether all scenes support the same key attributes.
    /// When <see langword="true"/>, <see cref="SupportedKeyAttributesPerScene"/> contains a single entry
    /// that applies to all scenes. <see langword="null"/> for version 1 payloads.
    /// </summary>
    bool? Identical,

    /// <summary>
    /// The supported key attributes for each scene.
    /// For version 1, this is <see langword="null"/>.
    /// For version 2+, when <see cref="Identical"/> is <see langword="true"/>, this contains one entry
    /// that applies to all scenes. Otherwise, it contains one entry per scene.
    /// </summary>
    IReadOnlyList<IReadOnlySet<CentralSceneKeyAttribute>>? SupportedKeyAttributesPerScene);

public sealed partial class CentralSceneCommandClass
{
    /// <summary>
    /// Gets the supported scenes report received from the device.
    /// </summary>
    public CentralSceneSupportedReport? SupportedReport { get; private set; }

    /// <summary>
    /// Request the supported scenes and key attributes from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The supported scenes report.</returns>
    public async Task<CentralSceneSupportedReport> GetSupportedAsync(CancellationToken cancellationToken)
    {
        CentralSceneSupportedGetCommand command = CentralSceneSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<CentralSceneSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        CentralSceneSupportedReport report = CentralSceneSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedReport = report;
        return report;
    }

    internal readonly struct CentralSceneSupportedGetCommand : ICommand
    {
        public CentralSceneSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static CentralSceneSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new CentralSceneSupportedGetCommand(frame);
        }
    }

    internal readonly struct CentralSceneSupportedReportCommand : ICommand
    {
        public CentralSceneSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static CentralSceneSupportedReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: Supported Scenes (1 byte)
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Central Scene Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Central Scene Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte supportedScenes = span[0];

            // Version 1: Only Supported Scenes, no bitmask data
            if (span.Length < 2)
            {
                return new CentralSceneSupportedReport(
                    supportedScenes,
                    SlowRefreshSupport: null,
                    Identical: null,
                    SupportedKeyAttributesPerScene: null);
            }

            // Version 2+: Properties1 byte layout:
            //   Bit 7: Slow Refresh Support (V3, reserved in V2)
            //   Bits 6-3: Reserved
            //   Bits 2-1: Number of Bit Mask Bytes
            //   Bit 0: Identical
            byte properties1 = span[1];

            // Per forward-compatibility: do not mask reserved bits for Slow Refresh Support
            bool slowRefreshSupport = (properties1 & 0b1000_0000) != 0;
            int numberOfBitMaskBytes = (properties1 >> 1) & 0b0000_0011;
            bool identical = (properties1 & 0b0000_0001) != 0;

            // Parse supported key attributes bitmasks
            int sceneBitmaskCount = identical ? 1 : supportedScenes;
            int expectedBitmaskDataLength = sceneBitmaskCount * numberOfBitMaskBytes;

            if (span.Length < 2 + expectedBitmaskDataLength)
            {
                logger.LogWarning(
                    "Central Scene Supported Report frame bitmask data is too short (expected {Expected}, got {Actual} bytes)",
                    expectedBitmaskDataLength,
                    span.Length - 2);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Central Scene Supported Report frame bitmask data is too short");
            }

            List<IReadOnlySet<CentralSceneKeyAttribute>> keyAttributesPerScene = new(sceneBitmaskCount);
            for (int sceneIndex = 0; sceneIndex < sceneBitmaskCount; sceneIndex++)
            {
                int offset = 2 + (sceneIndex * numberOfBitMaskBytes);
                ReadOnlySpan<byte> bitMask = span.Slice(offset, numberOfBitMaskBytes);
                keyAttributesPerScene.Add(BitMaskHelper.ParseBitMask<CentralSceneKeyAttribute>(bitMask));
            }

            return new CentralSceneSupportedReport(
                supportedScenes,
                slowRefreshSupport,
                identical,
                keyAttributesPerScene);
        }
    }
}
