using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ManufacturerSpecificCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ManufacturerSpecificCommandClass.ManufacturerSpecificGetCommand command =
            ManufacturerSpecificCommandClass.ManufacturerSpecificGetCommand.Create();

        Assert.AreEqual(CommandClassId.ManufacturerSpecific, ManufacturerSpecificCommandClass.ManufacturerSpecificGetCommand.CommandClassId);
        Assert.AreEqual((byte)ManufacturerSpecificCommand.Get, ManufacturerSpecificCommandClass.ManufacturerSpecificGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_ValidFrame()
    {
        // CC=0x72, Cmd=0x05, ManufacturerId=0x0086 (Aeotec), ProductTypeId=0x0003, ProductId=0x006C
        byte[] data = [0x72, 0x05, 0x00, 0x86, 0x00, 0x03, 0x00, 0x6C];
        CommandClassFrame frame = new(data);

        ManufacturerInformation info = ManufacturerSpecificCommandClass.ManufacturerSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0x0086, info.ManufacturerId);
        Assert.AreEqual((ushort)0x0003, info.ProductTypeId);
        Assert.AreEqual((ushort)0x006C, info.ProductId);
    }

    [TestMethod]
    public void Report_Parse_MaxValues()
    {
        // CC=0x72, Cmd=0x05, ManufacturerId=0xFFFF, ProductTypeId=0xFFFF, ProductId=0xFFFF
        byte[] data = [0x72, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        ManufacturerInformation info = ManufacturerSpecificCommandClass.ManufacturerSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0xFFFF, info.ManufacturerId);
        Assert.AreEqual((ushort)0xFFFF, info.ProductTypeId);
        Assert.AreEqual((ushort)0xFFFF, info.ProductId);
    }

    [TestMethod]
    public void Report_Parse_ZeroValues()
    {
        // CC=0x72, Cmd=0x05, ManufacturerId=0x0000, ProductTypeId=0x0000, ProductId=0x0000
        byte[] data = [0x72, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        ManufacturerInformation info = ManufacturerSpecificCommandClass.ManufacturerSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ushort)0x0000, info.ManufacturerId);
        Assert.AreEqual((ushort)0x0000, info.ProductTypeId);
        Assert.AreEqual((ushort)0x0000, info.ProductId);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x72, Cmd=0x05, only 4 bytes of parameters (need 6)
        byte[] data = [0x72, 0x05, 0x00, 0x86, 0x00, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void Report_Parse_EmptyParameters_Throws()
    {
        // CC=0x72, Cmd=0x05, no parameters
        byte[] data = [0x72, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DeviceSpecificGetCommand_Create_FactoryDefault()
    {
        ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand command =
            ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand.Create(ManufacturerSpecificDeviceIdType.FactoryDefault);

        Assert.AreEqual(CommandClassId.ManufacturerSpecific, ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand.CommandClassId);
        Assert.AreEqual((byte)ManufacturerSpecificCommand.DeviceSpecificGet, ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void DeviceSpecificGetCommand_Create_SerialNumber()
    {
        ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand command =
            ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand.Create(ManufacturerSpecificDeviceIdType.SerialNumber);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void DeviceSpecificGetCommand_Create_PseudoRandom()
    {
        ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand command =
            ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificGetCommand.Create(ManufacturerSpecificDeviceIdType.PseudoRandom);

        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_Utf8Format()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: Reserved=0, DeviceIdType=1 (SerialNumber) => 0x01
        // Byte1: Format=0 (UTF-8), Length=5 => 0b000_00101 = 0x05
        // Bytes2-6: "ABCDE" in UTF-8
        byte[] data = [0x72, 0x07, 0x01, 0x05, 0x41, 0x42, 0x43, 0x44, 0x45];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.SerialNumber, report.DeviceIdType);
        Assert.AreEqual("ABCDE", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_BinaryFormat()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: Reserved=0, DeviceIdType=0 (FactoryDefault) => 0x00
        // Byte1: Format=1 (Binary), Length=4 => 0b001_00100 = 0x24
        // Bytes2-5: 0x30, 0x31, 0x32, 0x33
        byte[] data = [0x72, 0x07, 0x00, 0x24, 0x30, 0x31, 0x32, 0x33];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.FactoryDefault, report.DeviceIdType);
        Assert.AreEqual("0x30313233", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_PseudoRandomType()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: Reserved=0, DeviceIdType=2 (PseudoRandom) => 0x02
        // Byte1: Format=0 (UTF-8), Length=3 => 0x03
        // Bytes2-4: "ABC"
        byte[] data = [0x72, 0x07, 0x02, 0x03, 0x41, 0x42, 0x43];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.PseudoRandom, report.DeviceIdType);
        Assert.AreEqual("ABC", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_ReservedFormatTreatedAsBinary()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: DeviceIdType=1 (SerialNumber) => 0x01
        // Byte1: Format=2 (reserved), Length=2 => 0b010_00010 = 0x42
        // Bytes2-3: 0xAB, 0xCD
        byte[] data = [0x72, 0x07, 0x01, 0x42, 0xAB, 0xCD];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.SerialNumber, report.DeviceIdType);
        Assert.AreEqual("0xABCD", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_ReservedBitsInDeviceIdType_Ignored()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: Reserved bits set (0b11111_001) = 0xF9, DeviceIdType=1 (SerialNumber)
        // Byte1: Format=0 (UTF-8), Length=1 => 0x01
        // Byte2: "X"
        byte[] data = [0x72, 0x07, 0xF9, 0x01, 0x58];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.SerialNumber, report.DeviceIdType);
        Assert.AreEqual("X", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_TooShort_NoParameters_Throws()
    {
        // CC=0x72, Cmd=0x07, no parameters
        byte[] data = [0x72, 0x07];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_TooShort_OneByte_Throws()
    {
        // CC=0x72, Cmd=0x07, only 1 byte of parameters
        byte[] data = [0x72, 0x07, 0x01];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_ZeroDataLength_Throws()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: DeviceIdType=1 => 0x01
        // Byte1: Format=0, Length=0 => 0x00 (spec says MUST NOT be zero)
        byte[] data = [0x72, 0x07, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_TruncatedData_Throws()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: DeviceIdType=0 => 0x00
        // Byte1: Format=0, Length=5 => 0x05
        // Only 2 bytes of data instead of 5
        byte[] data = [0x72, 0x07, 0x00, 0x05, 0x41, 0x42];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_SingleByteData()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: DeviceIdType=0 (FactoryDefault) => 0x00
        // Byte1: Format=0 (UTF-8), Length=1 => 0x01
        // Byte2: "A"
        byte[] data = [0x72, 0x07, 0x00, 0x01, 0x41];
        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.FactoryDefault, report.DeviceIdType);
        Assert.AreEqual("A", report.DeviceId);
    }

    [TestMethod]
    public void DeviceSpecificReport_Parse_MaxLengthData()
    {
        // CC=0x72, Cmd=0x07
        // Byte0: DeviceIdType=1 => 0x01
        // Byte1: Format=1 (Binary), Length=31 (max 5-bit value) => 0b001_11111 = 0x3F
        // 31 bytes of data
        byte[] data = new byte[2 + 2 + 31]; // CC + Cmd + 2 header bytes + 31 data bytes
        data[0] = 0x72;
        data[1] = 0x07;
        data[2] = 0x01; // DeviceIdType = SerialNumber
        data[3] = 0x3F; // Format=1 (Binary), Length=31
        for (int i = 0; i < 31; i++)
        {
            data[4 + i] = (byte)i;
        }

        CommandClassFrame frame = new(data);

        DeviceSpecificReport report = ManufacturerSpecificCommandClass.ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ManufacturerSpecificDeviceIdType.SerialNumber, report.DeviceIdType);
        Assert.IsTrue(report.DeviceId.StartsWith("0x", StringComparison.Ordinal));
    }
}
