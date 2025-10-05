using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace EvilOctane.SourceGenerators.Enum
{
    internal sealed class EnumSyntaxReceiver : ISyntaxReceiver
    {
        public readonly List<EnumDeclarationSyntax> EnumList = [];

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case EnumDeclarationSyntax enumDeclarationSyntax:
                    EnumList.Add(enumDeclarationSyntax);
                    break;
            }
        }
    }
}
