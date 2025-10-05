using EvilOctane.SourceGenerators.Type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvilOctane.SourceGenerators.Symbol
{
    public static class SymbolUtility
    {
        public static INamedTypeSymbol CastToNamed(ITypeSymbol typeSymbol)
        {
            return typeSymbol as INamedTypeSymbol ?? throw new InvalidCastException($"{nameof(typeSymbol)} is expected to be {nameof(INamedTypeSymbol)}.");
        }

        public static ITypeSymbol UnwrapNullable(INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated ?
                namedTypeSymbol.TypeArguments[0] :
                throw new ArgumentException("Nullable expected.", nameof(namedTypeSymbol));
        }

        public static bool TryGetAttributeSingle(TypeNameDictionary typeNameDictionary, ISymbol symbol, TypeName attributeType, out AttributeData? attributeData)
        {
            foreach (AttributeData symbolAttributeData in symbol.GetAttributes())
            {
                if (symbolAttributeData.AttributeClass == null)
                {
                    continue;
                }

                TypeName attributeTypeActual = typeNameDictionary.GetOrRegisterTypeName(symbolAttributeData.AttributeClass);

                if (attributeTypeActual.Equals(attributeType))
                {
                    attributeData = symbolAttributeData;
                    return true;
                }
            }

            attributeData = null;
            return false;
        }

        public static void GetFieldSymbols(INamedTypeSymbol symbol, List<IFieldSymbol> outFieldSymbols)
        {
            outFieldSymbols.Clear();

            foreach (ISymbol memberSymbol in symbol.GetMembers())
            {
                if (memberSymbol is IFieldSymbol fieldSymbol)
                {
                    outFieldSymbols.Add(fieldSymbol);
                }
            }
        }

        public static void GetDeclarationModifiers(INamedTypeSymbol namedTypeSymbol, out bool isPartial, out bool isRecord, out bool isReadonly, out bool isRef)
        {
            isPartial = false;
            isRecord = false;
            isReadonly = false;
            isRef = false;

            foreach (SyntaxReference syntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is BaseTypeDeclarationSyntax declaration)
                {
                    foreach (SyntaxToken modifier in declaration.Modifiers)
                    {
                        if (modifier.IsKind(SyntaxKind.PartialKeyword))
                        {
                            isPartial = true;
                        }

                        if (modifier.IsKind(SyntaxKind.RecordKeyword))
                        {
                            isRecord = true;
                        }

                        if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
                        {
                            isReadonly = true;
                        }

                        if (modifier.IsKind(SyntaxKind.RefKeyword))
                        {
                            isRef = true;
                        }
                    }
                }
            }
        }

        public static string GetDeclarationPrefix(INamedTypeSymbol namedTypeSymbol)
        {
            GetDeclarationModifiers(namedTypeSymbol, out bool isPartial, out bool isRecord, out bool isReadonly, out bool isRef);

            StringBuilder sb = new(namedTypeSymbol.DeclaredAccessibility.ToQualifier());
            _ = sb.Append(' ');

            if (isReadonly)
            {
                _ = sb.Append("readonly ");
            }

            if (isRef)
            {
                _ = sb.Append("ref ");
            }

            if (isPartial)
            {
                _ = sb.Append("partial ");
            }

            if (isRecord)
            {
                _ = sb.Append("record ");
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                _ = sb.Append("class");
            }
            else if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                _ = sb.Append("interface");
            }
            else if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                _ = sb.Append("struct");
            }

            return sb.ToString();
        }

        public static int GetTypeHierarchyDepth(INamedTypeSymbol namedTypeSymbol)
        {
            int depth = 1;

            for (INamedTypeSymbol? containingTypeSymbol = namedTypeSymbol.ContainingType; containingTypeSymbol is not null; ++depth)
            {
                containingTypeSymbol = containingTypeSymbol.ContainingType;
            }

            return depth;
        }

        public static INamedTypeSymbol[] GetTypeHierarchy(INamedTypeSymbol namedTypeSymbol)
        {
            int depth = GetTypeHierarchyDepth(namedTypeSymbol);

            INamedTypeSymbol[] result = new INamedTypeSymbol[depth];
            INamedTypeSymbol containingType = namedTypeSymbol;

            for (int index = 0; index != depth; ++index)
            {
                result[depth - index - 1] = containingType!;
                containingType = containingType!.ContainingType;
            }

            return result;
        }
    }
}
