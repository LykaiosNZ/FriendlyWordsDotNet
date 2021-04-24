namespace FriendlyWordsDotNet.SourceGenerators.Tests
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal static class Extensions
    {
        public static IPropertySymbol? GetProperty(this ITypeSymbol typeSymbol, string propertyName) => typeSymbol.GetMembers(propertyName).OfType<IPropertySymbol>().SingleOrDefault();
        public static ITypeSymbol GetSymbolForType<T>(this Compilation compilation) => compilation.GetSymbolForType(typeof(T))!;
        public static ITypeSymbol? GetSymbolForType(this Compilation compilation, Type type)
        {
            if (type.FullName == null)
            {
                return null;
            }

            if (!type.IsGenericType)
            {
                return compilation.GetTypeByMetadataName(type.FullName);
            }

            Type genericType = type.GetGenericTypeDefinition();
            Type[] genericArgs = type.GetGenericArguments();

            INamedTypeSymbol genericTypeSymbol = compilation.GetTypeByMetadataName(genericType.FullName!)!;
            ITypeSymbol[] genericArgSymbols = genericArgs.Select(t => compilation.GetSymbolForType(t)!).ToArray();

            return genericTypeSymbol.Construct(genericArgSymbols);
        }
    }
}
