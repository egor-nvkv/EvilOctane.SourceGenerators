using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EvilOctane.SourceGenerators.Type
{
    public readonly struct TypeNameDictionary
    {
        private readonly Dictionary<ITypeSymbol, TypeName> dictionary;

        private TypeNameDictionary(bool _)
        {
            dictionary = [];
        }

        public static TypeNameDictionary Create()
        {
            return new TypeNameDictionary(default);
        }

        private static ImmutableArray<string> GetCommaSeparatedNamespace(ITypeSymbol typeSymbol)
        {
            INamespaceSymbol @namespace = typeSymbol.ContainingNamespace;

            if (@namespace.IsGlobalNamespace)
            {
                return ImmutableArray<string>.Empty;
            }

            List<string> namespaces = [];

            do
            {
                namespaces.Add(@namespace.Name);
                @namespace = @namespace.ContainingNamespace;
            }
            while (!@namespace.IsGlobalNamespace);

            namespaces.Reverse();
            return namespaces.ToImmutableArray();
        }

        public readonly TypeName GetOrRegisterTypeName(ITypeSymbol typeSymbol)
        {
            if (dictionary.TryGetValue(typeSymbol, out TypeName? typeName))
            {
                return typeName!;
            }

            ImmutableArray<TypeName> genericTypeArguments = GetGenericTypeArguments(typeSymbol);

            if (typeSymbol.ContainingType == null)
            {
                // Not contained
                typeName = new TypeName(GetCommaSeparatedNamespace(typeSymbol), typeSymbol.Name, genericTypeArguments);
            }
            else
            {
                // Contained
                typeName = new TypeName(GetOrRegisterTypeName(typeSymbol.ContainingType), typeSymbol.Name, genericTypeArguments);
            }

            dictionary[typeSymbol] = typeName;
            return typeName;
        }

        private readonly ImmutableArray<TypeName> GetGenericTypeArguments(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return ImmutableArray<TypeName>.Empty;
            }

            int arity = namedTypeSymbol.Arity;

            if (arity == 0)
            {
                return ImmutableArray<TypeName>.Empty;
            }

            TypeName[] genericTypeArguments = new TypeName[arity];

            for (int index = 0; index != arity; ++index)
            {
                ITypeSymbol typeArgument = namedTypeSymbol.TypeArguments[index];
                genericTypeArguments[index] = GetOrRegisterTypeName(typeArgument);
            }

            return genericTypeArguments.ToImmutableArray();
        }
    }
}
