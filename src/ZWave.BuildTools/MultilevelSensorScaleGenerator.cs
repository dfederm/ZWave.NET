using System.Text;
using Microsoft.CodeAnalysis;

namespace ZWave.BuildTools;

[Generator]
public sealed class MultilevelSensorScaleGenerator : ConfigGeneratorBase<ScaleTypeConfigs>
{
    protected override string ConfigType => "MultilevelSensorScales";

    protected override string CreateSource(ScaleTypeConfigs config)
    {
        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

namespace ZWave.CommandClasses;

public sealed class MultilevelSensorScale
{
    internal MultilevelSensorScale(byte id, string label, string? unit)
    {
        Id = id;
        Label = label;
        Unit = unit;
    }

    public byte Id { get; }

    public string Label { get; }

    public string? Unit { get; }

");

        foreach (ScaleTypeConfig scaleTypeConfig in config.ScaleTypes)
        {
            sb.Append($@"
    internal static MultilevelSensorScale?[] {scaleTypeConfig.Name} {{ get; }}
        = ");

            AppendScalesArray(sb, scaleTypeConfig.Scales, indentLevel: 2);

            sb.AppendLine(";");
        }

        sb.Append(@"
}
");
        return sb.ToString();
    }

    internal static void AppendScalesArray(
        StringBuilder sb,
        IReadOnlyList<ScaleConfig> scales,
        int indentLevel)
    {
        static void Indent(StringBuilder sb, int indentLevel) => sb.Append(' ', indentLevel * 4);

        // Determine array size from max scale ID + 1
        int maxId = 0;
        foreach (ScaleConfig scale in scales)
        {
            int id = Convert.ToInt32(scale.Id, 16);
            if (id > maxId)
            {
                maxId = id;
            }
        }

        int arraySize = maxId + 1;

        // Build a sparse array indexed by scale ID
        var slotted = new ScaleConfig?[arraySize];
        foreach (ScaleConfig scale in scales)
        {
            int id = Convert.ToInt32(scale.Id, 16);
            slotted[id] = scale;
        }

        sb.AppendLine("new MultilevelSensorScale?[]");

        Indent(sb, indentLevel);
        sb.AppendLine("{");

        for (int i = 0; i < arraySize; i++)
        {
            Indent(sb, indentLevel + 1);
            ScaleConfig? scale = slotted[i];
            if (scale != null)
            {
                sb.Append($@"new MultilevelSensorScale({scale.Id}, ""{scale.Label}"", ");
                if (scale.Unit == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append($@"""{scale.Unit}""");
                }

                sb.AppendLine("),");
            }
            else
            {
                sb.AppendLine("null,");
            }
        }

        Indent(sb, indentLevel);
        sb.Append('}');
    }
}
