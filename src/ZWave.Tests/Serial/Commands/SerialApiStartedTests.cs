using ZWave.Serial.Commands;
using ZWave.Serial;

namespace ZWave.Tests.Serial.Commands;

[TestClass]
public class SerialApiStartedTests
{
    internal record SerialApiStartedRequestData(
        SerialApiStartedWakeUpReason WakeUpReason,
        bool WatchdogStarted,
        byte DeviceOptionMask,
        byte GenericDeviceType,
        byte SpecificDeviceType,
        ReadOnlyMemory<byte> CommandClasses,
        SerialApiStartedSupportedProtocols SupportedProtocols);

    private static IEnumerable<object[]> RequestData
    {
        get
        {
            return new[]
            {
                new object[] {
                    new byte[] { 0x05, 0x00, 0x01, 0x02, 0x01, 0x00 },
                    new SerialApiStartedRequestData(
                        WakeUpReason: SerialApiStartedWakeUpReason.PowerUp,
                        WatchdogStarted: false,
                        DeviceOptionMask: 0x01,
                        GenericDeviceType: 0x02,
                        SpecificDeviceType: 0x01,
                        CommandClasses: ReadOnlyMemory<byte>.Empty,
                        SupportedProtocols: 0x00)
                },

                // Synthetic test for CCs
                new object[] {
                    new byte[] { 0x05, 0x00, 0x01, 0x02, 0x01, 0x03, 0x01, 0x02, 0x03 },
                    new SerialApiStartedRequestData(
                        WakeUpReason: SerialApiStartedWakeUpReason.PowerUp,
                        WatchdogStarted: false,
                        DeviceOptionMask: 0x01,
                        GenericDeviceType: 0x02,
                        SpecificDeviceType: 0x01,
                        CommandClasses: new byte[] { 0x01, 0x02, 0x03 },
                        SupportedProtocols: 0x00)
                },

                // Synthetic test for supported protocols
                new object[] {
                    new byte[] { 0x05, 0x00, 0x01, 0x02, 0x01, 0x00, 0x01 },
                    new SerialApiStartedRequestData(
                        WakeUpReason: SerialApiStartedWakeUpReason.PowerUp,
                        WatchdogStarted: false,
                        DeviceOptionMask: 0x01,
                        GenericDeviceType: 0x02,
                        SpecificDeviceType: 0x01,
                        CommandClasses: ReadOnlyMemory<byte>.Empty,
                        SupportedProtocols: SerialApiStartedSupportedProtocols.ZWaveLongRange)
                },
            };
        }
    }

    [TestMethod]
    public void RequestStatics()
    {
        Assert.AreEqual(DataFrameType.REQ, SerialApiStartedRequest.Type);
        Assert.AreEqual(CommandId.SerialApiStarted, SerialApiStartedRequest.CommandId);
    }

    [TestMethod]
    [DynamicData("RequestData")]
    public void Request(byte[] commandParameters, object dataObj)
    {
        // Cast to circumvent inconsistent accessibility of types
        SerialApiStartedRequestData data = (SerialApiStartedRequestData)dataObj;

        var dataFrame = DataFrame.Create(DataFrameType.REQ, CommandId.SerialApiStarted, commandParameters);
        var request = new SerialApiStartedRequest(dataFrame);

        Assert.AreEqual(data.WakeUpReason, request.WakeUpReason);
        Assert.AreEqual(data.WatchdogStarted, request.WatchdogStarted);
        Assert.AreEqual(data.DeviceOptionMask, request.DeviceOptionMask);
        Assert.AreEqual(data.GenericDeviceType, request.GenericDeviceType);
        Assert.AreEqual(data.SpecificDeviceType, request.SpecificDeviceType);
        Assert.That.MemoryIsEqual(data.CommandClasses, request.CommandClasses);
        Assert.AreEqual(data.SupportedProtocols, request.SupportedProtocols);
    }
}
