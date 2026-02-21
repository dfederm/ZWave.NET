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

Implemented command classes include Basic, Battery, Binary Sensor, Binary Switch, Color Switch, Manufacturer Specific, Multilevel Sensor, Multilevel Switch, Notification, Powerlevel, Version, Wake Up, and Z-Wave Plus Info. Unimplemented command classes reported by a device are tracked and do not block node interviews.

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