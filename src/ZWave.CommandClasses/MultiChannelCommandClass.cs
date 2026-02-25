using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Multi Channel Command Class commands (version 4).
/// </summary>
public enum MultiChannelCommand : byte
{
    /// <summary>
    /// Request the number of End Points implemented by a node.
    /// </summary>
    EndpointGet = 0x07,

    /// <summary>
    /// Report the number of End Points implemented by a node.
    /// </summary>
    EndpointReport = 0x08,

    /// <summary>
    /// Request the capabilities of a specific End Point.
    /// </summary>
    CapabilityGet = 0x09,

    /// <summary>
    /// Report the capabilities of a specific End Point.
    /// </summary>
    CapabilityReport = 0x0A,

    /// <summary>
    /// Request End Points matching a specific device class.
    /// </summary>
    EndpointFind = 0x0B,

    /// <summary>
    /// Report End Points matching a specific device class.
    /// </summary>
    EndpointFindReport = 0x0C,

    /// <summary>
    /// Encapsulate a command for a specific End Point.
    /// </summary>
    CommandEncapsulation = 0x0D,

    /// <summary>
    /// Request the members of an Aggregated End Point.
    /// </summary>
    AggregatedMembersGet = 0x0E,

    /// <summary>
    /// Report the members of an Aggregated End Point.
    /// </summary>
    AggregatedMembersReport = 0x0F,
}

/// <summary>
/// Implements the Multi Channel Command Class (version 4).
/// </summary>
/// <remarks>
/// The Multi Channel Command Class is used to address one or more End Points in a
/// Multi Channel device. Per spec §4.2.2, a Multi Channel device may implement 1 to 127
/// End Points, each with its own set of supported command classes.
/// </remarks>
[CommandClass(CommandClassId.MultiChannel)]
public sealed partial class MultiChannelCommandClass : CommandClass<MultiChannelCommand>
{
    internal MultiChannelCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(MultiChannelCommand command)
        => command switch
        {
            MultiChannelCommand.EndpointGet => true,
            MultiChannelCommand.CapabilityGet => true,
            MultiChannelCommand.EndpointFind => EffectiveVersion >= 3,
            MultiChannelCommand.CommandEncapsulation => true,
            MultiChannelCommand.AggregatedMembersGet => EffectiveVersion >= 4,
            _ => false,
        };

    internal override CommandClassCategory Category => CommandClassCategory.Transport;

    /// <summary>
    /// Per spec §6.4.2.1, the Multi Channel CC interview discovers endpoints and their CCs.
    /// </summary>
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Step 1: Get the number of endpoints
        MultiChannelEndpointReport endpointReport = await GetEndpointReportAsync(cancellationToken).ConfigureAwait(false);

        if (endpointReport.IndividualEndpointCount > 0)
        {
            // Step 2: Discover endpoint indices. Endpoint indices are not necessarily sequential,
            // so use EndpointFind with wildcards to get the actual indices when available (v3+).
            // Fall back to assuming sequential indices for older versions.
            IReadOnlyList<byte> endpointIndices;
            if (IsCommandSupported(MultiChannelCommand.EndpointFind) == true)
            {
                endpointIndices = await FindEndpointsAsync(0xFF, 0xFF, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                byte[] indices = new byte[endpointReport.IndividualEndpointCount];
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = (byte)(i + 1);
                }

                endpointIndices = indices;
            }

            // Step 3: For each endpoint, get its capabilities.
            // Per spec §4.2.2.4: If Identical is set, all endpoints have same device class and CCs.
            MultiChannelCapabilityReport firstCapability = await GetCapabilityAsync(endpointIndices[0], cancellationToken).ConfigureAwait(false);

            if (endpointReport.AreIdentical)
            {
                // All endpoints have the same capabilities as the first, so clone the report for
                // each additional endpoint without sending additional GetCapability commands.
                for (int i = 1; i < endpointIndices.Count; i++)
                {
                    MultiChannelCapabilityReport clonedCapability = new(
                        firstCapability.IsDynamic,
                        endpointIndices[i],
                        firstCapability.GenericDeviceClass,
                        firstCapability.SpecificDeviceClass,
                        firstCapability.CommandClasses);
                    OnCapabilityReportReceived?.Invoke(clonedCapability);
                }
            }
            else
            {
                for (int i = 1; i < endpointIndices.Count; i++)
                {
                    await GetCapabilityAsync(endpointIndices[i], cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Per spec §6.4.2.1: "A controlling node MAY skip the interview of aggregated End Points"
        // Aggregated End Points are also deprecated per spec §4.2.3.1.
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((MultiChannelCommand)frame.CommandId)
        {
            case MultiChannelCommand.EndpointReport:
            {
                MultiChannelEndpointReport report = MultiChannelEndpointReportCommand.Parse(frame, Logger);
                LastEndpointReport = report;
                OnEndpointReportReceived?.Invoke(report);
                break;
            }
            case MultiChannelCommand.CapabilityReport:
            {
                MultiChannelCapabilityReport capability = MultiChannelCapabilityReportCommand.Parse(frame, Logger);
                OnCapabilityReportReceived?.Invoke(capability);
                break;
            }
            case MultiChannelCommand.CommandEncapsulation:
            {
                MultiChannelCommandEncapsulation encapsulation = CommandEncapsulationCommand.Parse(frame, Logger);
                OnCommandEncapsulationReceived?.Invoke(encapsulation);
                break;
            }
        }
    }
}
