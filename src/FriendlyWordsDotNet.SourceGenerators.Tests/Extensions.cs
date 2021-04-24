using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace FriendlyWordsDotNet.SourceGenerators.Tests
{
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

            var genericType = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            var genericTypeSymbol = compilation.GetTypeByMetadataName(genericType.FullName!)!;
            var genericArgSymbols = genericArgs.Select(t => compilation.GetSymbolForType(t)!).ToArray();

            return genericTypeSymbol.Construct(genericArgSymbols);
        }
    }
}
