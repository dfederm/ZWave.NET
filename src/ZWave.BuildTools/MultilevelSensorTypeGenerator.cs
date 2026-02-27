using System.Text;
using Microsoft.CodeAnalysis;

namespace ZWave.BuildTools;

[Generator]
public sealed class MultilevelSensorTypeGenerator : ConfigGeneratorBase<SensorTypeConfigs>
{
    protected override string ConfigType => "MultilevelSensorTypes";

    protected override string CreateSource(SensorTypeConfigs config)
    {
        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace ZWave.CommandClasses;

public enum MultilevelSensorType : byte
{
");

        IReadOnlyList<SensorTypeConfig> sensorTypes = config.SensorTypes;

        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            sb.Append($@"
    {sensorType.Name} = {sensorType.Id},");
        }

        sb.Append(@"
}

public static class MultilevelSensorTypeExtensions
{
");

        // Emit private static readonly fields for inline scale arrays
        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            if (sensorType.Scales != null)
            {
                sb.Append($@"    private static readonly MultilevelSensorScale?[] s_{CamelCase(sensorType.Name)}Scales = ");
                MultilevelSensorScaleGenerator.AppendScalesArray(sb, sensorType.Scales, indentLevel: 1);
                sb.AppendLine(";");
                sb.AppendLine();
            }
        }

        // ToDisplayString — switch expression
        sb.Append(@"    public static string ToDisplayString(this MultilevelSensorType sensorType)
        => sensorType switch
        {
");

        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            sb.AppendLine($@"            MultilevelSensorType.{sensorType.Name} => ""{sensorType.DisplayName}"",");
        }

        sb.Append(@"            _ => ""Unknown"",
        };

");

        // GetScale — switch for outer lookup, array index for inner
        sb.Append(@"    public static MultilevelSensorScale GetScale(this MultilevelSensorType sensorType, byte scaleId)
    {
        MultilevelSensorScale?[]? scales = sensorType switch
        {
");

        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            if (sensorType.ScaleType != null)
            {
                sb.AppendLine($"            MultilevelSensorType.{sensorType.Name} => MultilevelSensorScale.{sensorType.ScaleType},");
            }
            else if (sensorType.Scales != null)
            {
                sb.AppendLine($"            MultilevelSensorType.{sensorType.Name} => s_{CamelCase(sensorType.Name)}Scales,");
            }
        }

        sb.Append(@"            _ => null,
        };

        if (scales != null && scaleId < scales.Length)
        {
            MultilevelSensorScale? scale = scales[scaleId];
            if (scale != null)
            {
                return scale;
            }
        }

        return new MultilevelSensorScale(scaleId, ""Unknown"", null);
    }
}
");
        return sb.ToString();
    }

    private static string CamelCase(string name)
        => char.ToLowerInvariant(name[0]) + name.Substring(1);
}
