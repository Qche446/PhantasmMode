using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace FargosPhantasmMode.SourceGenerator;
public static class RegisterIDTable
{
    public static ImmutableArray<ModTypeInfo> ScanAssembly(Compilation compilation, string targetAssemblyName, string ModTypeMetadataName)
    {
        var result = ImmutableArray.CreateBuilder<ModTypeInfo>();

        var modBuffSymbol = compilation.GetTypeByMetadataName(ModTypeMetadataName);

        if (modBuffSymbol is null)
            return result.ToImmutable();

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            // 只扫描指定名称的程序集。
            if (!string.Equals(assembly.Identity.Name, targetAssemblyName, StringComparison.OrdinalIgnoreCase))
                continue;

            CollectModTypeTypes(assembly.GlobalNamespace, modBuffSymbol, result);
        }

        return result.ToImmutable();
    }
    private static void CollectModTypeTypes(INamespaceSymbol ns, INamedTypeSymbol modBuffSymbol, ImmutableArray<ModTypeInfo>.Builder result)
    {
        foreach (var type in ns.GetTypeMembers())
            CollectType(type, modBuffSymbol, result);

        foreach (var childNs in ns.GetNamespaceMembers())
            CollectModTypeTypes(childNs, modBuffSymbol, result);
    }
    public static void CollectType(INamedTypeSymbol type, INamedTypeSymbol modBuffSymbol, ImmutableArray<ModTypeInfo>.Builder result)
    {
        var current = type.BaseType;

        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, modBuffSymbol.OriginalDefinition))
            {
                if (!type.IsAbstract && !type.IsGenericType && !type.IsStatic && type.DeclaredAccessibility == Accessibility.Public)
                {
                    result.Add(new ModTypeInfo(type.Name, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
                break;
            }

            current = current.BaseType;
        }
        foreach (var nested in type.GetTypeMembers())
            CollectType(nested, modBuffSymbol, result);
    }


    ////
    public enum Modtype : byte
    {
        ModBuff, 
        ModProjectile, 
        ModNPC
    }
}
public sealed class ModTypeInfo(string name, string typeName)
{
    public string Name { get; } = name;
    public string TypeName { get; } = typeName;
}
