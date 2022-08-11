using System.Collections;
using System.Reflection;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Serial.Commands;

public class CommandTestBase
{
    internal static void TestSendableCommand<TCommand>(
        DataFrameType dataFrameType,
        CommandId commandId,
        IReadOnlyList<(TCommand Command, byte[] ExpectedCommandParameters)> tests)
        where TCommand : struct, ICommand<TCommand>
    {
        Assert.AreEqual(dataFrameType, TCommand.Type);
        Assert.AreEqual(commandId, TCommand.CommandId);

        foreach ((TCommand command, byte[] expectedCommandParameters) in tests)
        {
            Assert.AreEqual(dataFrameType, command.Frame.Type);
            Assert.AreEqual(commandId, command.Frame.CommandId);
            Assert.That.MemoryIsEqual(expectedCommandParameters, command.Frame.CommandParameters);
        }
    }

    internal static void TestReceivableCommand<TCommand, TData>(
        DataFrameType dataFrameType,
        CommandId commandId,
        IReadOnlyList<(byte[] CommandParameters, TData ExpectedData)> tests)
        where TCommand : struct, ICommand<TCommand>
    {
        Assert.AreEqual(dataFrameType, TCommand.Type);

        if (commandId == 0)
        {
            Assert.ThrowsException<InvalidOperationException>(() => TCommand.CommandId);
        }
        else
        {
            Assert.AreEqual(commandId, TCommand.CommandId);
        }

        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        PropertyInfo[] commandProperties = typeof(TCommand).GetProperties(bindingFlags)
            .Where(p => !p.Name.Equals("Frame", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        PropertyInfo[] dataProperties = typeof(TData).GetProperties(bindingFlags);
        Assert.AreEqual(
            commandProperties.Length,
            dataProperties.Length,
            $"Command and data property counts do not match: {commandProperties.Length} vs {dataProperties.Length}");

        var properties = new List<(PropertyInfo CommandProperty, PropertyInfo DataProperty)>(commandProperties.Length);
        foreach (PropertyInfo dataProperty in dataProperties)
        {
            PropertyInfo? commandProperty = commandProperties.FirstOrDefault(
                commandProperty => commandProperty.Name.Equals(dataProperty.Name, StringComparison.Ordinal) && commandProperty.PropertyType == dataProperty.PropertyType);

            Assert.IsNotNull(commandProperty, $"Could not find matching property in command: {dataProperty.PropertyType.Name} {dataProperty.Name}");

            properties.Add((commandProperty, dataProperty));
        }

        foreach ((ReadOnlyMemory<byte> commandParameters, TData expectedData) in tests)
        {
            var dataFrame = DataFrame.Create(dataFrameType, commandId, commandParameters.Span);
            var command = TCommand.Create(dataFrame);

            foreach ((PropertyInfo commandProperty, PropertyInfo dataProperty) in properties)
            {
                object? expectedValue = dataProperty.GetValue(expectedData);
                object? actualValue = commandProperty.GetValue(command);

                string propertyName = commandProperty.Name;
                Type propertyType = commandProperty.PropertyType;
                if (propertyType == typeof(ReadOnlyMemory<byte>))
                {
                    Assert.That.MemoryIsEqual((ReadOnlyMemory<byte>)expectedValue!, (ReadOnlyMemory<byte>)actualValue!, $"Property '{propertyName}' not equal");
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    Type[] genericArgs = propertyType.GetGenericArguments();
                    Type listType = typeof(List<>).MakeGenericType(genericArgs);
                    Type enumerableType = typeof(IEnumerable<>).MakeGenericType(genericArgs);
                    ConstructorInfo listCtor = listType.GetConstructor(new[] { enumerableType })!;

                    ICollection expectedValueList = (ICollection)listCtor.Invoke(new[] { expectedValue });
                    ICollection actualValueList = (ICollection)listCtor.Invoke(new[] { actualValue });

                    CollectionAssert.AreEquivalent(expectedValueList, actualValueList, $"Property '{propertyName}' not equal");
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                {
                    Type[] genericArgs = propertyType.GetGenericArguments();
                    Type listType = typeof(List<>).MakeGenericType(genericArgs);
                    Type enumerableType = typeof(IEnumerable<>).MakeGenericType(genericArgs);
                    ConstructorInfo listCtor = listType.GetConstructor(new[] { enumerableType })!;

                    ICollection expectedValueList = (ICollection)listCtor.Invoke(new[] { expectedValue });
                    ICollection actualValueList = (ICollection)listCtor.Invoke(new[] { actualValue });

                    MethodInfo stringJoin = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(mi => mi.Name.Equals("Join", StringComparison.OrdinalIgnoreCase)
                            && mi.GetParameters().Length == 2
                            && mi.GetParameters()[0].ParameterType == typeof(string)
                            && mi.IsGenericMethodDefinition)
                        .MakeGenericMethod(genericArgs);

                    CollectionAssert.AreEqual(
                        expectedValueList,
                        actualValueList,
                        $"Property '{propertyName}' not equal.{Environment.NewLine}"
                            + $"  Expected: {stringJoin.Invoke(null, new object[] { ", ", expectedValueList })}{Environment.NewLine}"
                            + $"  Actual:   {stringJoin.Invoke(null, new object[] { ", ", actualValueList })}{Environment.NewLine}");
                }
                else
                {
                    Assert.AreEqual(expectedValue, actualValue, $"Property '{propertyName}' not equal");
                }
            }
        }
    }
}
