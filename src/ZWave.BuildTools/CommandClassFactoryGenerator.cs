using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZWave.BuildTools;

[Generator]
public sealed class CommandClassFactoryGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor DuplicateCommandClassId = new DiagnosticDescriptor(
        id: "ZWAVE001",
        title: "Found duplicate command id",
        messageFormat: "Found multiple classes claiming to handle the command class '{0}'",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string AttributeSource = @"
namespace ZWave.CommandClasses;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class CommandClassAttribute: Attribute
{
    public CommandClassId Id { get; }

    public CommandClassAttribute(CommandClassId id)
        => (Id) = (id);
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource("CommandClassAttribute.generated.cs", AttributeSource));

        // Find all classes with [CommandClass] and extract (CommandClassId, ClassName)
        IncrementalValuesProvider<(string CommandClassId, string ClassName)> commandClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZWave.CommandClasses.CommandClassAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    string className = ((ClassDeclarationSyntax)ctx.TargetNode).Identifier.ToString();

                    // Get the attribute argument as "CommandClassId.EnumMemberName"
                    foreach (AttributeData attr in ctx.Attributes)
                    {
                        if (attr.ConstructorArguments.Length > 0)
                        {
                            TypedConstant arg = attr.ConstructorArguments[0];
                            if (arg.Type?.TypeKind == TypeKind.Enum)
                            {
                                // Get enum member name from the constant value
                                string? memberName = arg.Type.GetMembers()
                                    .OfType<IFieldSymbol>()
                                    .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, arg.Value))
                                    ?.Name;

                                if (memberName != null)
                                {
                                    return ($"CommandClassId.{memberName}", className);
                                }
                            }
                        }
                    }

                    return default;
                })
            .Where(static x => x.Item1 != null!);

        IncrementalValueProvider<ImmutableArray<(string CommandClassId, string ClassName)>> collected = commandClasses.Collect();

        context.RegisterSourceOutput(collected, static (spc, items) =>
        {
            var idToType = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach ((string commandClassId, string className) in items)
            {
                if (idToType.ContainsKey(commandClassId))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(DuplicateCommandClassId, Location.None, commandClassId));
                }
                else
                {
                    idToType.Add(commandClassId, className);
                }
            }

            spc.AddSource("CommandClassFactory.generated.cs", GenerateSource(idToType));
        });
    }

    private static string GenerateSource(Dictionary<string, string> idToType)
    {
        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

internal static class CommandClassFactory
{
    private static readonly Dictionary<CommandClassId, Func<CommandClassInfo, IDriver, IEndpoint, ILogger, CommandClass>> Constructors = new Dictionary<CommandClassId, Func<CommandClassInfo, IDriver, IEndpoint, ILogger, CommandClass>>
    {
");

        foreach (KeyValuePair<string, string> pair in idToType)
        {
            sb.AppendLine($"        {{ {pair.Key}, (info, driver, endpoint, logger) => new {pair.Value}(info, driver, endpoint, logger) }},");
        }

        sb.Append(@"    };

    private static readonly Dictionary<Type, CommandClassId> TypeToIdMap = new Dictionary<Type, CommandClassId>
    {
");

        foreach (KeyValuePair<string, string> pair in idToType)
        {
            sb.AppendLine($"        {{ typeof({pair.Value}), {pair.Key} }},");
        }

        sb.Append(@"    };

    public static CommandClass Create(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        => Constructors.TryGetValue(info.CommandClass, out Func<CommandClassInfo, IDriver, IEndpoint, ILogger, CommandClass>? constructor)
            ? constructor(info, driver, endpoint, logger)
            : new NotImplementedCommandClass(info, driver, endpoint, logger);

    public static CommandClassId GetCommandClassId<TCommandClass>()
        where TCommandClass : CommandClass
        => TypeToIdMap[typeof(TCommandClass)];
}
");
        return sb.ToString();
    }
}
