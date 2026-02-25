namespace ZWave.CommandClasses;

/// <summary>
/// Categorizes command classes per the Z-Wave Application Specification.
/// </summary>
/// <remarks>
/// The interview order follows the spec recommendation (CL:0000.00.22.01.1):
/// Management CCs first, then Transport/Encapsulation CCs, then Application CCs.
/// This ensures that capabilities (Version CC), encapsulation layers (Multi Channel,
/// Security), and endpoint discovery are all resolved before application CCs are interviewed.
/// </remarks>
public enum CommandClassCategory
{
    /// <summary>
    /// Management Command Classes (spec §6.3).
    /// Interviewed first. Includes Version, Z-Wave Plus Info, Wake Up, Association,
    /// Manufacturer Specific, etc.
    /// </summary>
    Management,

    /// <summary>
    /// Transport Encapsulation Command Classes (spec §6.4).
    /// Interviewed after Management CCs. Includes Multi Channel, Security, CRC-16, etc.
    /// Multi Channel CC discovers endpoints during this phase.
    /// </summary>
    Transport,

    /// <summary>
    /// Application Command Classes (spec §6.2).
    /// Interviewed last, after endpoints are discovered. Includes Binary Switch,
    /// Multilevel Sensor, Notification, Battery, etc.
    /// </summary>
    Application,
}
