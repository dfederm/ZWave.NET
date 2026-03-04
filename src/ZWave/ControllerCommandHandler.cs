using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;

namespace ZWave;

/// <summary>
/// Handles incoming commands from other nodes that are directed at the controller.
/// This implements the "supporting side" of command classes — responding to queries
/// about the controller's own state (association groups, version info, etc.).
/// </summary>
internal sealed class ControllerCommandHandler
{
    /// <summary>
    /// The Device Reset Locally Notification command ID (0x01).
    /// </summary>
    private const byte DeviceResetLocallyNotificationCommandId = 0x01;

    private readonly Controller _controller;
    private readonly Driver _driver;
    private readonly ILogger _logger;

    internal ControllerCommandHandler(Controller controller, Driver driver, ILogger logger)
    {
        _controller = controller;
        _driver = driver;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches an incoming command to the appropriate handler.
    /// Called from <see cref="Controller.HandleCommand"/> when a command is received
    /// from another node (i.e., not from the controller itself).
    /// </summary>
    internal void HandleCommand(CommandClassFrame frame, ushort sourceNodeId)
    {
        switch (frame.CommandClassId)
        {
            case CommandClassId.Association:
            {
                HandleAssociationCommand(frame, sourceNodeId);
                break;
            }
            case CommandClassId.AssociationGroupInformation:
            {
                HandleAGICommand(frame, sourceNodeId);
                break;
            }
        }
    }

    #region Response Helpers

    private void SendResponse<T>(T response, ushort destinationNodeId) where T : struct, ICommand
    {
        _ = SendResponseAsync(response, destinationNodeId);
    }

    private async Task SendResponseAsync<T>(T response, ushort destinationNodeId)
        where T : struct, ICommand
    {
        try
        {
            await _driver.SendCommandAsync(response, destinationNodeId, 0, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogControllerResponseFailed(destinationNodeId, ex);
        }
    }

    #endregion

    #region Association CC Handlers

    private void HandleAssociationCommand(CommandClassFrame frame, ushort sourceNodeId)
    {
        AssociationCommand command = (AssociationCommand)frame.CommandId;
        switch (command)
        {
            case AssociationCommand.Get:
            {
                HandleAssociationGet(sourceNodeId);
                break;
            }
            case AssociationCommand.Set:
            {
                HandleAssociationSet(frame);
                break;
            }
            case AssociationCommand.Remove:
            {
                HandleAssociationRemove(frame);
                break;
            }
            case AssociationCommand.SupportedGroupingsGet:
            {
                HandleAssociationSupportedGroupingsGet(sourceNodeId);
                break;
            }
            case AssociationCommand.SpecificGroupGet:
            {
                HandleAssociationSpecificGroupGet(sourceNodeId);
                break;
            }
        }
    }

    private void HandleAssociationGet(ushort sourceNodeId)
    {
        // Per CC:0085.01.02.12.001: return info for group 1 for unsupported group IDs.
        // The controller only supports group 1 (Lifeline).
        const byte groupId = 1;

        List<byte> associations = _controller.Associations;

        AssociationCommandClass.AssociationReportCommand report =
            AssociationCommandClass.AssociationReportCommand.Create(
                groupId,
                Controller.MaxAssociationDestinations,
                reportsToFollow: 0,
                [.. associations]);
        SendResponse(report, sourceNodeId);
    }

    private void HandleAssociationSet(CommandClassFrame frame)
    {
        if (frame.CommandParameters.Length < 1)
        {
            return;
        }

        ReadOnlySpan<byte> span = frame.CommandParameters.Span;
        byte groupId = span[0];

        // We only support the lifeline group (group 1).
        // Per CC:0085.01.01.11.003: ignore unsupported grouping identifiers.
        if (groupId != 1)
        {
            return;
        }

        ReadOnlySpan<byte> nodeIds = span[1..];
        List<byte> associations = _controller.Associations;
        for (int i = 0; i < nodeIds.Length; i++)
        {
            byte nodeId = nodeIds[i];
            if (nodeId == 0)
            {
                continue;
            }

            if (!associations.Contains(nodeId))
            {
                // Per CC:0085.01.01.13.001: MAY be ignored if the group is already full.
                if (associations.Count >= Controller.MaxAssociationDestinations)
                {
                    break;
                }

                associations.Add(nodeId);
            }
        }
    }

    private void HandleAssociationRemove(CommandClassFrame frame)
    {
        if (frame.CommandParameters.Length < 1)
        {
            return;
        }

        ReadOnlySpan<byte> span = frame.CommandParameters.Span;
        byte groupId = span[0];

        // Per CC:0085.02.04.11.003: ignore unsupported grouping identifiers,
        // except 0 which MUST be accepted (targets all groups).
        if (groupId != 0 && groupId != 1)
        {
            return;
        }

        ReadOnlySpan<byte> nodeIds = span[1..];
        List<byte> associations = _controller.Associations;
        if (nodeIds.Length == 0)
        {
            // Remove all destinations from the group (or all groups if groupId=0).
            associations.Clear();
        }
        else
        {
            for (int i = 0; i < nodeIds.Length; i++)
            {
                associations.Remove(nodeIds[i]);
            }
        }
    }

    private void HandleAssociationSupportedGroupingsGet(ushort sourceNodeId)
    {
        // The controller has exactly 1 association group (Lifeline).
        AssociationCommandClass.AssociationSupportedGroupingsReportCommand report =
            AssociationCommandClass.AssociationSupportedGroupingsReportCommand.Create(1);
        SendResponse(report, sourceNodeId);
    }

    private void HandleAssociationSpecificGroupGet(ushort sourceNodeId)
    {
        // Per CC:0085.02.0C.12.002: return 0 if not supported.
        AssociationCommandClass.AssociationSpecificGroupReportCommand report =
            AssociationCommandClass.AssociationSpecificGroupReportCommand.Create(0);
        SendResponse(report, sourceNodeId);
    }

    #endregion

    #region Association Group Information CC Handlers

    private void HandleAGICommand(CommandClassFrame frame, ushort sourceNodeId)
    {
        AssociationGroupInformationCommand command = (AssociationGroupInformationCommand)frame.CommandId;
        switch (command)
        {
            case AssociationGroupInformationCommand.GroupNameGet:
            {
                HandleAGIGroupNameGet(sourceNodeId);
                break;
            }
            case AssociationGroupInformationCommand.GroupInfoGet:
            {
                HandleAGIGroupInfoGet(frame, sourceNodeId);
                break;
            }
            case AssociationGroupInformationCommand.CommandListGet:
            {
                HandleAGICommandListGet(sourceNodeId);
                break;
            }
        }
    }

    private void HandleAGIGroupNameGet(ushort sourceNodeId)
    {
        // Per CC:0059.01.01.12.001: return info for group 1 for unsupported group IDs.
        // Per CC:0059.01.00.11.006: root device lifeline group MUST be named "Lifeline".
        const byte groupId = 1;

        AssociationGroupInformationCommandClass.GroupNameReportCommand report =
            AssociationGroupInformationCommandClass.GroupNameReportCommand.Create(groupId, "Lifeline");
        SendResponse(report, sourceNodeId);
    }

    private void HandleAGIGroupInfoGet(CommandClassFrame frame, ushort sourceNodeId)
    {
        // Parse the List Mode flag from the request.
        bool listMode = false;
        if (frame.CommandParameters.Length >= 1)
        {
            listMode = (frame.CommandParameters.Span[0] & 0b0100_0000) != 0;
        }

        // Per CC:0059.01.00.11.005: lifeline profile MUST be advertised for group 1.
        // Profile = General:Lifeline (0x00, 0x01).
        AssociationGroupInfo[] groups =
        [
            new AssociationGroupInfo(1, new AssociationGroupProfile(0x00, 0x01)),
        ];

        AssociationGroupInformationCommandClass.GroupInfoReportCommand report =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Create(
                listMode,
                dynamicInfo: false,
                groups);
        SendResponse(report, sourceNodeId);
    }

    private void HandleAGICommandListGet(ushort sourceNodeId)
    {
        // Per CC:0059.01.05.12.002: return info for group 1 for unsupported group IDs.
        const byte groupId = 1;

        // The controller's lifeline group sends Device Reset Locally Notification.
        AssociationGroupCommand[] commands =
        [
            new AssociationGroupCommand(
                (ushort)CommandClassId.DeviceResetLocally,
                DeviceResetLocallyNotificationCommandId),
        ];

        AssociationGroupInformationCommandClass.CommandListReportCommand report =
            AssociationGroupInformationCommandClass.CommandListReportCommand.Create(groupId, commands);
        SendResponse(report, sourceNodeId);
    }

    #endregion
}
