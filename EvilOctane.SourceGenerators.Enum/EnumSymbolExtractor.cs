using EvilOctane.SourceGenerators.Symbol;
using EvilOctane.SourceGenerators.Type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace EvilOctane.SourceGenerators.Enum
{
    internal static class EnumSymbolExtractor
    {
        public static void GetEligibleForCodeGeneration(GeneratorExecutionContext context, TypeNameDictionary typeNameDictionary, List<EnumDeclarationSyntax> enumList, Dictionary<INamedTypeSymbol, AttributeData> outSymbols)
        {
            outSymbols.Clear();

            Compilation compilation = context.Compilation;
            TypeName attribute = new("EvilOctane.SourceGeneration", "GenerateEnumExtensionCode");

            foreach (EnumDeclarationSyntax enumDeclarationSyntax in enumList)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol namedTypeSymbol)
                {
                    continue;
                }
                else if (outSymbols.ContainsKey(namedTypeSymbol))
                {
                    continue;
                }

                if (SymbolUtility.TryGetAttributeSingle(typeNameDictionary, namedTypeSymbol, attribute, out AttributeData? attributeData))
                {
                    outSymbols[namedTypeSymbol] = attributeData!;
                }
            }
        }
    }
}
