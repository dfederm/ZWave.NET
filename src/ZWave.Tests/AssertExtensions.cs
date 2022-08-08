using System.Reflection;

namespace ZWave.Tests;

internal static class AssertExtensions
{
    public static void ConstructorValidatesNull<T>(this Assert _, params object[] parameters)
    {
        // Get the correct constructor. Note that we can't use the GetConstructor method since
        // out parameters are of some concrete types and the constructor may take a base class or interface.
        Type type = typeof(T);
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? matchingConstructor = null;
        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length != parameters.Length)
            {
                continue;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].GetType();
                if (!parameterType.IsInstanceOfType(constructorParameters[i].ParameterType))
                {
                    break;
                }
            }

            matchingConstructor = constructor;
        }

        Assert.IsNotNull(matchingConstructor, "Could not find constructor with the given parameter types.");

        // Sanity check the non-null case
        var constructedObject = matchingConstructor.Invoke(parameters);
        Assert.IsNotNull(constructedObject);
        Assert.IsInstanceOfType(constructedObject, type);

        // FOr each parameter, ensure it's null-checked
        for (int i = 0; i < parameters.Length; i++)
        {
            // Skip parameters which were given as null. This implies they're allowed to be null,
            // as proven by the successfully constructed object above.
            if (parameters[i] == null)
            {
                continue;
            }

            // Skip structs as they can't be null.
            if (!parameters[i].GetType().IsClass)
            {
                continue;
            }

            // Force the parameter in the i-th slot to be null
            var parametersCopy = (object[])parameters.Clone();
            parametersCopy[i] = null!;

            ParameterInfo parameterUnderTest = matchingConstructor.GetParameters()[i];
            try
            {
                matchingConstructor.Invoke(parametersCopy);

                // The construction should have failed.
                Assert.Fail($"The parameter `{parameterUnderTest.Name}` was not null-checked");
            }
            // When using ConstructorInfo.Invoke, the exception in wrapped in a TargetInvocationException
            catch (TargetInvocationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentNullException));

                string actualParamName = ((ArgumentNullException)ex.InnerException!).ParamName!;
                Assert.AreEqual(
                    parameterUnderTest.Name,
                    actualParamName,
                    "Parameter threw an ArgumentNullException with the wrong param name");
            }
        }
    }

    public static void MemoryIsEqual(this Assert _, ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        => Assert.IsTrue(
            expected.Span.SequenceEqual(actual.Span),
            $"Sequences are not equal!{Environment.NewLine}"
                + $"  Expected: 0x{Convert.ToHexString(expected.Span)}{Environment.NewLine}"
                + $"  Actual:   0x{Convert.ToHexString(actual.Span)}{Environment.NewLine}");
}
