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
| Scene Activation | 0x2B | — | — | ✅ |
| Scene Actuator Configuration | 0x2C | ✅ | ✅ | ✅ |
| Scene Controller Configuration | 0x2D | ✅ | ✅ | ✅ |
| Binary Sensor | 0x30 | ✅ | — | ✅ |
| Multilevel Sensor | 0x31 | ✅ | — | ✅ |
| Meter | 0x32 | ✅ | — | ✅ |
| Color Switch | 0x33 | ✅ | ✅ | ✅ |
| Thermostat Mode | 0x40 | ✅ | ✅ | ✅ |
| Thermostat Operating State | 0x42 | ✅ | — | ✅ |
| Thermostat Setpoint | 0x43 | ✅ | ✅ | ✅ |
| Thermostat Fan Mode | 0x44 | ✅ | ✅ | ✅ |
| Thermostat Fan State | 0x45 | ✅ | — | ✅ |
| Association Group Information | 0x59 | ✅ | — | ✅ |
| Device Reset Locally | 0x5A | — | — | ✅ |
| Central Scene | 0x5B | ✅ | ✅ | ✅ |
| Z-Wave Plus Info | 0x5E | ✅ | — | ✅ |
| Door Lock | 0x62 | ✅ | ✅ | ✅ |
| User Code | 0x63 | ✅ | ✅ | ✅ |
| Humidity Control Setpoint | 0x64 | ✅ | ✅ | ✅ |
| Barrier Operator | 0x66 | ✅ | ✅ | ✅ |
| Window Covering | 0x6A | ✅ | ✅ | ✅ |
| Humidity Control Mode | 0x6D | ✅ | ✅ | ✅ |
| Humidity Control Operating State | 0x6E | ✅ | — | ✅ |
| Entry Control | 0x6F | — | ✅ | ✅ |
| Configuration | 0x70 | ✅ | ✅ | ✅ |
| Notification | 0x71 | ✅ | ✅ | ✅ |
| Manufacturer Specific | 0x72 | ✅ | — | ✅ |
| Powerlevel | 0x73 | ✅ | ✅ | ✅ |
| Protection | 0x75 | ✅ | ✅ | ✅ |
| Node Naming and Location | 0x77 | ✅ | ✅ | ✅ |
| Sound Switch | 0x79 | ✅ | ✅ | ✅ |
| Battery | 0x80 | ✅ | — | ✅ |
| Clock | 0x81 | ✅ | ✅ | ✅ |
| Wake Up | 0x84 | ✅ | ✅ | ✅ |
| Association | 0x85 | ✅ | ✅ | ✅ |
| Version | 0x86 | ✅ | — | ✅ |
| Indicator | 0x87 | ✅ | ✅ | ✅ |
| Multi Channel Association | 0x8E | ✅ | ✅ | ✅ |

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