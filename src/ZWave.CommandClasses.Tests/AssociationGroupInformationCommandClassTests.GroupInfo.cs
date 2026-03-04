using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class AssociationGroupInformationCommandClassTests
{
    [TestMethod]
    public void GroupInfoGetCommand_Create_ListMode_HasCorrectFormat()
    {
        AssociationGroupInformationCommandClass.GroupInfoGetCommand command =
            AssociationGroupInformationCommandClass.GroupInfoGetCommand.Create(listMode: true, groupingIdentifier: 0);

        Assert.AreEqual(
            CommandClassId.AssociationGroupInformation,
            AssociationGroupInformationCommandClass.GroupInfoGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)AssociationGroupInformationCommand.GroupInfoGet,
            AssociationGroupInformationCommandClass.GroupInfoGetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + Flags + GroupId
        // Flags: List Mode bit = 0b0100_0000
        Assert.AreEqual((byte)0b0100_0000, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GroupInfoGetCommand_Create_SingleGroup_HasCorrectFormat()
    {
        AssociationGroupInformationCommandClass.GroupInfoGetCommand command =
            AssociationGroupInformationCommandClass.GroupInfoGetCommand.Create(listMode: false, groupingIdentifier: 3);

        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + Flags + GroupId
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]); // No flags
        Assert.AreEqual((byte)3, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_SingleGroup()
    {
        // CC=0x59, Cmd=0x04
        // Flags: ListMode=0, DynamicInfo=0, GroupCount=1
        // Group 1: GroupId=1, Mode=0, ProfileMSB=0x00 (General), ProfileLSB=0x01 (Lifeline), Reserved=0, EventCode=0x0000
        byte[] data = [0x59, 0x04, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        (bool dynamicInfo, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(dynamicInfo);
        Assert.HasCount(1, groups);
        Assert.AreEqual((byte)1, groups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x00, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_MultipleGroups()
    {
        // CC=0x59, Cmd=0x04
        // Flags: ListMode=1, DynamicInfo=0, GroupCount=3
        // Group 1: GroupId=1, Mode=0, Profile=General:Lifeline (0x00, 0x01), Res=0, Event=0x0000
        // Group 2: GroupId=2, Mode=0, Profile=Control:Key1 (0x20, 0x01), Res=0, Event=0x0000
        // Group 3: GroupId=3, Mode=0, Profile=Control:Key1 (0x20, 0x01), Res=0, Event=0x0000
        byte[] data =
        [
            0x59, 0x04,
            0b1000_0011, // ListMode=1, DynamicInfo=0, GroupCount=3
            0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // Group 1
            0x02, 0x00, 0x20, 0x01, 0x00, 0x00, 0x00, // Group 2
            0x03, 0x00, 0x20, 0x01, 0x00, 0x00, 0x00, // Group 3
        ];
        CommandClassFrame frame = new(data);

        (bool dynamicInfo, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(dynamicInfo);
        Assert.HasCount(3, groups);

        Assert.AreEqual((byte)1, groups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x00, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);

        Assert.AreEqual((byte)2, groups[1].GroupingIdentifier);
        Assert.AreEqual((byte)0x20, groups[1].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[1].Profile.Identifier);

        Assert.AreEqual((byte)3, groups[2].GroupingIdentifier);
        Assert.AreEqual((byte)0x20, groups[2].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[2].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_DynamicInfo()
    {
        // CC=0x59, Cmd=0x04
        // Flags: ListMode=0, DynamicInfo=1, GroupCount=1
        // Group 1: GroupId=1, Mode=0, Profile=General:Lifeline, Res=0, Event=0x0000
        byte[] data =
        [
            0x59, 0x04,
            0b0100_0001, // ListMode=0, DynamicInfo=1, GroupCount=1
            0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
        ];
        CommandClassFrame frame = new(data);

        (bool dynamicInfo, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(dynamicInfo);
        Assert.HasCount(1, groups);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_SensorProfile()
    {
        // CC=0x59, Cmd=0x04
        // Flags: ListMode=0, DynamicInfo=0, GroupCount=1
        // Group 1: GroupId=2, Mode=0, Profile=Sensor:Temperature (0x31, 0x01), Res=0, Event=0x0000
        byte[] data =
        [
            0x59, 0x04,
            0x01, // GroupCount=1
            0x02, 0x00, 0x31, 0x01, 0x00, 0x00, 0x00,
        ];
        CommandClassFrame frame = new(data);

        (bool dynamicInfo, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(dynamicInfo);
        Assert.HasCount(1, groups);
        Assert.AreEqual((byte)2, groups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x31, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_NotificationProfile()
    {
        // Group with Notification:SmokeAlarm profile (0x71, 0x01)
        byte[] data =
        [
            0x59, 0x04,
            0x01, // GroupCount=1
            0x02, 0x00, 0x71, 0x01, 0x00, 0x00, 0x00,
        ];
        CommandClassFrame frame = new(data);

        (bool _, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, groups);
        Assert.AreEqual((byte)0x71, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_MeterProfile()
    {
        // Group with Meter:Electric profile (0x32, 0x01) - v2+
        byte[] data =
        [
            0x59, 0x04,
            0x01, // GroupCount=1
            0x02, 0x00, 0x32, 0x01, 0x00, 0x00, 0x00,
        ];
        CommandClassFrame frame = new(data);

        (bool _, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, groups);
        Assert.AreEqual((byte)0x32, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_ZeroGroups()
    {
        // CC=0x59, Cmd=0x04, Flags: GroupCount=0
        byte[] data = [0x59, 0x04, 0x00];
        CommandClassFrame frame = new(data);

        (bool dynamicInfo, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(dynamicInfo);
        Assert.IsEmpty(groups);
    }

    [TestMethod]
    public void GroupInfoReport_Parse_TooShort_Throws()
    {
        // CC=0x59, Cmd=0x04, no parameters
        byte[] data = [0x59, 0x04];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void GroupInfoReport_Parse_TruncatedGroups_Throws()
    {
        // CC=0x59, Cmd=0x04, GroupCount=2, but only 1 group entry (7 bytes)
        byte[] data =
        [
            0x59, 0x04,
            0x02, // GroupCount=2
            0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // Only 1 group
        ];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void GroupInfoReport_Parse_IgnoresReservedFields()
    {
        // Mode, Reserved, and Event Code fields should be ignored per spec.
        // Set them to non-zero values to verify they don't affect parsing.
        byte[] data =
        [
            0x59, 0x04,
            0x01, // GroupCount=1
            0x01, 0xFF, 0x00, 0x01, 0xAB, 0xCD, 0xEF, // Non-zero mode, reserved, event code
        ];
        CommandClassFrame frame = new(data);

        (bool _, List<AssociationGroupInfo> groups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, groups);
        Assert.AreEqual((byte)1, groups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x00, groups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, groups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Create_ParseRoundTrip_SingleGroup()
    {
        AssociationGroupInfo[] groups =
        [
            new AssociationGroupInfo(1, new AssociationGroupProfile(0x00, 0x01)),
        ];

        AssociationGroupInformationCommandClass.GroupInfoReportCommand report =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Create(
                listMode: false, dynamicInfo: false, groups);

        (bool dynamicInfo, List<AssociationGroupInfo> parsedGroups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(
                report.Frame, NullLogger.Instance);

        Assert.IsFalse(dynamicInfo);
        Assert.HasCount(1, parsedGroups);
        Assert.AreEqual((byte)1, parsedGroups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x00, parsedGroups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, parsedGroups[0].Profile.Identifier);
    }

    [TestMethod]
    public void GroupInfoReport_Create_ParseRoundTrip_ListMode()
    {
        AssociationGroupInfo[] groups =
        [
            new AssociationGroupInfo(1, new AssociationGroupProfile(0x00, 0x01)),
            new AssociationGroupInfo(2, new AssociationGroupProfile(0x20, 0x01)),
        ];

        AssociationGroupInformationCommandClass.GroupInfoReportCommand report =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Create(
                listMode: true, dynamicInfo: true, groups);

        (bool dynamicInfo, List<AssociationGroupInfo> parsedGroups) =
            AssociationGroupInformationCommandClass.GroupInfoReportCommand.Parse(
                report.Frame, NullLogger.Instance);

        Assert.IsTrue(dynamicInfo);
        Assert.HasCount(2, parsedGroups);
        Assert.AreEqual((byte)1, parsedGroups[0].GroupingIdentifier);
        Assert.AreEqual((byte)0x00, parsedGroups[0].Profile.Category);
        Assert.AreEqual((byte)0x01, parsedGroups[0].Profile.Identifier);
        Assert.AreEqual((byte)2, parsedGroups[1].GroupingIdentifier);
        Assert.AreEqual((byte)0x20, parsedGroups[1].Profile.Category);
        Assert.AreEqual((byte)0x01, parsedGroups[1].Profile.Identifier);
    }
}
