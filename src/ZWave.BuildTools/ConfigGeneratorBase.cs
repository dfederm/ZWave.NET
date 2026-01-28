using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ZWave.BuildTools;

public abstract class ConfigGeneratorBase<TConfig> : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor MissingConfig = new DiagnosticDescriptor(
        id: "ZWAVE002",
        title: "Missing config file",
        messageFormat: "Could not find file for config type '{0}'",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidConfig = new DiagnosticDescriptor(
        id: "ZWAVE003",
        title: "Invalid config file",
        messageFormat: "Config type '{0}' was invalid: {1}",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    protected abstract string ConfigType { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter additional texts to find the matching config file and extract path + content
        IncrementalValueProvider<ImmutableArray<(string Path, string Content)>> configFiles = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair => pair.Right.GetOptions(pair.Left).TryGetValue("build_metadata.additionalfiles.ConfigType", out string? configType) && configType.Equals(ConfigType))
            .Select(static (pair, cancellationToken) => (Path: pair.Left.Path, Content: pair.Left.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .Where(static item => !string.IsNullOrEmpty(item.Content))
            .Collect();

        context.RegisterSourceOutput(configFiles, (sourceProductionContext, files) =>
        {
            if (files.Length == 0)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(MissingConfig, Location.None, ConfigType));
                return;
            }

            var configFile = files[0];
            TConfig? config;
            try
            {
                config = JsonSerializer.Deserialize<TConfig>(configFile.Content, JsonOptions);
            }
            catch (JsonException ex)
            {
                Location location;
                if (ex.LineNumber == null || ex.BytePositionInLine == null)
                {
                    location = Location.None;
                }
                else
                {
                    var linePosition = new LinePosition((int)ex.LineNumber.Value, (int)ex.BytePositionInLine.Value);
                var lineSpan = new LinePositionSpan(linePosition, linePosition);
                var textSpan = new TextSpan(0, 0);
                location = Location.Create(configFile.Path, textSpan, lineSpan);
                }

                var diagnostic = Diagnostic.Create(InvalidConfig, location, ConfigType, ex.Message);
                sourceProductionContext.ReportDiagnostic(diagnostic);
                return;
            }

            if (config == null)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(InvalidConfig, Location.None, ConfigType, "Json was invalid"));
                return;
            }

            sourceProductionContext.AddSource(ConfigType + ".generated.cs", CreateSource(config));
        });
    }

    protected abstract string CreateSource(TConfig config);
}
