# ZWave.NET

[![Build and Test](https://github.com/dfederm/ZWave.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/dfederm/ZWave.NET/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A .NET library for communicating with Z-Wave devices through a Z-Wave USB controller (e.g. Aeotec Z-Stick, UZB-7) using the Z-Wave Serial API protocol.

> **Note:** This project is under active development and is not yet published as a NuGet package.

## Features

- Full implementation of the Z-Wave Serial API frame protocol (SOF/ACK/NAK/CAN handshake, retransmission, checksum validation)
- Automatic controller identification and node discovery on startup
- Node interviewing with topological command class dependency resolution
- Async-first design using `System.IO.Pipelines`, `System.Threading.Channels`, and `CancellationToken` throughout

### Implemented Command Classes

| Command Class | ID | Get | Set | Report |
|---|---|---|---|---|
| Basic | 0x20 | ✅ | ✅ | ✅ |
| Binary Switch | 0x25 | ✅ | ✅ | ✅ |
| Multilevel Switch | 0x26 | ✅ | ✅ | ✅ |
| Binary Sensor | 0x30 | ✅ | — | ✅ |
| Multilevel Sensor | 0x31 | ✅ | — | ✅ |
| Color Switch | 0x33 | ✅ | ✅ | ✅ |
| Z-Wave Plus Info | 0x5E | ✅ | — | ✅ |
| Notification | 0x71 | ✅ | ✅ | ✅ |
| Manufacturer Specific | 0x72 | ✅ | — | ✅ |
| Powerlevel | 0x73 | ✅ | ✅ | ✅ |
| Battery | 0x80 | ✅ | — | ✅ |
| Wake Up | 0x84 | ✅ | ✅ | ✅ |
| Version | 0x86 | ✅ | — | ✅ |

Unimplemented command classes reported by a device are tracked as `NotImplementedCommandClass` and do not block node interviews.

## Quick Start

```csharp
using Microsoft.Extensions.Logging;
using ZWave;
using ZWave.CommandClasses;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = loggerFactory.CreateLogger<Driver>();

// Connect to the Z-Wave controller
await using Driver driver = await Driver.CreateAsync(logger, "COM3", CancellationToken.None);

// Access discovered nodes
foreach (var (nodeId, node) in driver.Controller.Nodes)
{
    Console.WriteLine($"Node {nodeId}: Interview status = {node.InterviewStatus}");
}

// Control a Binary Switch
var binarySwitch = driver.Controller.Nodes[2].GetCommandClass<BinarySwitchCommandClass>();
await binarySwitch.SetAsync(targetValue: true, duration: null, CancellationToken.None);
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.