using Microsoft.CodeAnalysis;

namespace TUnit.AutoMocker.Generators;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MissingGenerateMock = new(
        id: "AM0001",
        title: "Missing [GenerateMock] for constructor dependency",
        messageFormat: "Constructor parameter '{0}' of type '{1}' in '{2}' requires [assembly: GenerateMock(typeof({1}))] to be declared",
        category: "TUnit.AutoMocker",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The TUnit.Mocks source generator needs a [GenerateMock] attribute to generate a mock for this type. Add [assembly: GenerateMock(typeof({1}))] to your project.");

    public static readonly DiagnosticDescriptor NoAccessibleConstructor = new(
        id: "AM0002",
        title: "No accessible constructor found",
        messageFormat: "Type '{0}' has no accessible public constructor. Cannot generate auto-mock factory.",
        category: "TUnit.AutoMocker",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UsingGreediestConstructor = new(
        id: "AM0003",
        title: "Using greediest constructor",
        messageFormat: "Using constructor with {0} parameter(s) for '{1}'",
        category: "TUnit.AutoMocker",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConstructorTypesNotFound = new(
        id: "AM0004",
        title: "Specified constructor not found",
        messageFormat: "No constructor matching the specified ConstructorTypes found on '{0}'",
        category: "TUnit.AutoMocker",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
