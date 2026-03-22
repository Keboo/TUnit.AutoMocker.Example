using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.AutoMocker.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class AutoMockGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            Execute(spc, compilation);
        });
    }

    private static void Execute(SourceProductionContext spc, Compilation compilation)
    {
        var autoMockAttrType = compilation.GetTypeByMetadataName("TUnit.AutoMocker.AutoMockAttribute");
        if (autoMockAttrType is null)
            return;

        var generateMockAttrType = compilation.GetTypeByMetadataName("TUnit.Mocks.GenerateMockAttribute");
        var assemblyAttributes = compilation.Assembly.GetAttributes();

        // Collect all [GenerateMock] types for validation
        var generateMockTypes = new HashSet<string>(
#if NETSTANDARD2_0
            System.StringComparer.Ordinal
#else
            StringComparer.Ordinal
#endif
        );
        if (generateMockAttrType is not null)
        {
            foreach (var attr in assemblyAttributes)
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateMockAttrType))
                    continue;
                if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is INamedTypeSymbol gmType)
                {
                    generateMockTypes.Add(gmType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
        }

        // Process each [AutoMock] attribute
        foreach (var attr in assemblyAttributes)
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, autoMockAttrType))
                continue;

            if (attr.ConstructorArguments.Length < 1)
                continue;

            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol targetType)
                continue;

            // Check for optional ConstructorTypes
            ImmutableArray<ITypeSymbol>? ctorTypeSymbols = null;
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "ConstructorTypes" && !namedArg.Value.IsNull)
                {
                    var values = namedArg.Value.Values;
                    var typeSymbols = ImmutableArray.CreateBuilder<ITypeSymbol>(values.Length);
                    foreach (var v in values)
                    {
                        if (v.Value is ITypeSymbol ts)
                            typeSymbols.Add(ts);
                    }
                    ctorTypeSymbols = typeSymbols.ToImmutable();
                }
            }

            var targetFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var targetName = targetType.Name;

            // Find constructors
            var constructors = targetType.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .ToImmutableArray();

            if (constructors.Length == 0)
            {
                // No public constructors — still generate for parameterless implicit ctor
                GenerateFactory(spc, targetType, ImmutableArray<ConstructorParam>.Empty, generateMockTypes);
                continue;
            }

            IMethodSymbol? selectedCtor = null;

            if (ctorTypeSymbols is not null)
            {
                foreach (var ctor in constructors)
                {
                    if (ctor.Parameters.Length != ctorTypeSymbols.Value.Length)
                        continue;

                    bool match = true;
                    for (int i = 0; i < ctor.Parameters.Length; i++)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(ctor.Parameters[i].Type, ctorTypeSymbols.Value[i]))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        selectedCtor = ctor;
                        break;
                    }
                }

                if (selectedCtor is null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ConstructorTypesNotFound,
                        attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                        targetFullName));
                    continue;
                }
            }
            else
            {
                selectedCtor = constructors.OrderByDescending(c => c.Parameters.Length).First();
            }

            var parameters = selectedCtor.Parameters
                .Select(p => new ConstructorParam(p.Name, p.Type, IsMockable(p.Type)))
                .ToImmutableArray();

            // Report info diagnostic about constructor choice
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UsingGreediestConstructor,
                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                parameters.Length,
                targetFullName));

            // Validate that mockable dependencies have [GenerateMock] attributes
            foreach (var param in parameters)
            {
                if (param.IsMockable)
                {
                    var paramFullName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!generateMockTypes.Contains(paramFullName))
                    {
                        var paramDisplayName = param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        spc.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MissingGenerateMock,
                            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                            param.Name,
                            paramDisplayName,
                            targetName));
                    }
                }
            }

            GenerateFactory(spc, targetType, parameters, generateMockTypes);
        }
    }

    private static bool IsMockable(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
            return true;
        if (type.TypeKind == TypeKind.Class && type.IsAbstract)
            return true;
        return false;
    }

    private static void GenerateFactory(
        SourceProductionContext spc,
        INamedTypeSymbol targetType,
        ImmutableArray<ConstructorParam> parameters,
        HashSet<string> generateMockTypes)
    {
        var targetFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var safeTypeName = targetFullName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using TUnit.Mocks;");
        sb.AppendLine("using TUnit.AutoMocker;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.AutoMocker.Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    internal static class AutoMock_{safeTypeName}");
        sb.AppendLine("    {");
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        internal static void Register()");
        sb.AppendLine("        {");
        sb.AppendLine($"            AutoMocker.RegisterFactory<{targetFullName}>(behavior => CreateInstance(behavior));");
        sb.AppendLine($"            AutoMocker.RegisterBuilderFactory<{targetFullName}>((behavior, overrides) => CreateInstanceWithOverrides(behavior, overrides));");
        sb.AppendLine("        }");
        sb.AppendLine();

        // CreateInstance method
        sb.AppendLine($"        private static AutoMocked<{targetFullName}> CreateInstance(MockBehavior behavior)");
        sb.AppendLine("        {");
        sb.AppendLine("            var mocks = new Dictionary<Type, IMock>();");

        foreach (var param in parameters)
        {
            var paramFullName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (param.IsMockable)
            {
                sb.AppendLine($"            var mock_{param.Name} = Mock.Of<{paramFullName}>(behavior);");
                sb.AppendLine($"            mocks[typeof({paramFullName})] = mock_{param.Name};");
            }
        }

        sb.Append($"            var instance = new {targetFullName}(");
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            var param = parameters[i];
            if (param.IsMockable)
                sb.Append($"mock_{param.Name}.Object");
            else
                sb.Append($"default({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})!");
        }
        sb.AppendLine(");");

        sb.AppendLine($"            return new AutoMocked<{targetFullName}>(instance, mocks);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // CreateInstanceWithOverrides method
        sb.AppendLine($"        private static AutoMocked<{targetFullName}> CreateInstanceWithOverrides(MockBehavior behavior, Dictionary<Type, object> overrides)");
        sb.AppendLine("        {");
        sb.AppendLine("            var mocks = new Dictionary<Type, IMock>();");

        foreach (var param in parameters)
        {
            var paramFullName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (param.IsMockable)
            {
                sb.AppendLine($"            {paramFullName} val_{param.Name};");
                sb.AppendLine($"            if (overrides.TryGetValue(typeof({paramFullName}), out var override_{param.Name}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                if (override_{param.Name} is Mock<{paramFullName}> mockOverride_{param.Name})");
                sb.AppendLine("                {");
                sb.AppendLine($"                    mocks[typeof({paramFullName})] = mockOverride_{param.Name};");
                sb.AppendLine($"                    val_{param.Name} = mockOverride_{param.Name}.Object;");
                sb.AppendLine("                }");
                sb.AppendLine("                else");
                sb.AppendLine("                {");
                sb.AppendLine($"                    val_{param.Name} = ({paramFullName})override_{param.Name};");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("            else");
                sb.AppendLine("            {");
                sb.AppendLine($"                var mock_{param.Name} = Mock.Of<{paramFullName}>(behavior);");
                sb.AppendLine($"                mocks[typeof({paramFullName})] = mock_{param.Name};");
                sb.AppendLine($"                val_{param.Name} = mock_{param.Name}.Object;");
                sb.AppendLine("            }");
            }
        }

        sb.Append($"            var instance = new {targetFullName}(");
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            var param = parameters[i];
            if (param.IsMockable)
                sb.Append($"val_{param.Name}");
            else
                sb.Append($"default({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})!");
        }
        sb.AppendLine(");");

        sb.AppendLine($"            return new AutoMocked<{targetFullName}>(instance, mocks);");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource($"AutoMock_{safeTypeName}.g.cs", sb.ToString());
    }

    private sealed class ConstructorParam
    {
        public ConstructorParam(string name, ITypeSymbol type, bool isMockable)
        {
            Name = name;
            Type = type;
            IsMockable = isMockable;
        }

        public string Name { get; }
        public ITypeSymbol Type { get; }
        public bool IsMockable { get; }
    }
}
