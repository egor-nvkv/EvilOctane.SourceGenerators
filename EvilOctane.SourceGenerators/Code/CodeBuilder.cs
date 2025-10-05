using EvilOctane.SourceGenerators.Symbol;
using EvilOctane.SourceGenerators.Type;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvilOctane.SourceGenerators.Code
{
    public sealed class CodeBuilder
    {
        public const string Indent = "    ";

        private readonly StringBuilder stringBuilder = new();
        private readonly SortedSet<string> usings = [];

        private int indentLevel;

        public int IndentLevel
        {
            get => indentLevel;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Negative indent", nameof(value));
                }

                indentLevel = value;
            }
        }

        public override string ToString()
        {
            return stringBuilder.ToString();
        }

        public PragmaDisableArea BeginPragmaDisableArea(string pragma)
        {
            return new PragmaDisableArea(this, pragma);
        }

        public IndentedArea BeginIndentedArea()
        {
            return new IndentedArea(this);
        }

        public CurlyBracesArea BeginCurlyBracesArea()
        {
            return new CurlyBracesArea(this);
        }

        public CurlyBracesWithSemicolonArea BeginCurlyBracesWithSemicolonArea()
        {
            return new CurlyBracesWithSemicolonArea(this);
        }

        public UnsafeArea BeginUnsafeArea()
        {
            return new UnsafeArea(this);
        }

        public GenericTypeParametersArea BeginGenericTypeParametersArea()
        {
            return new GenericTypeParametersArea(this);
        }

        public TypeDeclarationArea BeginTypeDeclarationArea(INamedTypeSymbol namedTypeSymbol)
        {
            return new TypeDeclarationArea(this, namedTypeSymbol);
        }

        public void AddUsing(string @using)
        {
            _ = usings.Add(@using);
        }

        public void AddUsing(string @using, string relativeToNamespace)
        {
            if (!relativeToNamespace.StartsWith(@using))
            {
                _ = usings.Add(@using);
            }
        }

        public void AddUsings(string @using0, string @using1)
        {
            AddUsing(using0);
            AddUsing(using1);
        }

        public void AddUsings(string @using0, string @using1, string @using2)
        {
            AddUsing(using0);
            AddUsing(using1);
            AddUsing(using2);
        }

        public void AddUsings(params string[] usings)
        {
            this.usings.UnionWith(usings);
        }

        public void AddUsingsFor(TypeName typeName)
        {
            for (TypeName? iterator = typeName; iterator != null; iterator = iterator.ContainingType)
            {
                AddUsing(iterator.GetNamespace());

                foreach (TypeName genericTypeArgument in iterator.GenericTypeArguments)
                {
                    AddUsingsFor(genericTypeArgument);
                }
            }
        }

        public void Append(char c)
        {
            _ = stringBuilder.Append(c);
        }

        public void Append(string code)
        {
            _ = stringBuilder.Append(code);
        }

        public void Append(TypeName typeName)
        {
            AddUsingsFor(@typeName);
            typeName.ExpandWithGenericTypeArguments(stringBuilder, includeNamespaces: false);
        }

        public void AppendWithIndent(string code)
        {
            if (indentLevel != 0)
            {
                _ = stringBuilder.Insert(stringBuilder.Length, Indent, indentLevel);
            }

            _ = stringBuilder.Append(code);
        }

        public void AppendWithIndent(string code, string @using)
        {
            AddUsing(@using);

            if (indentLevel != 0)
            {
                _ = stringBuilder.Insert(stringBuilder.Length, Indent, indentLevel);
            }

            _ = stringBuilder.Append(code);
        }

        public void AppendLine()
        {
            _ = stringBuilder.AppendLine();
        }

        public void AppendLine(string code)
        {
            if (indentLevel != 0)
            {
                _ = stringBuilder.Insert(stringBuilder.Length, Indent, indentLevel);
            }

            _ = stringBuilder.AppendLine(code);
        }

        public void AppendLine(string code, string @using)
        {
            AddUsing(@using);
            AppendLine(code);
        }

        public void AppendLine(string code, string using0, string using1)
        {
            AddUsings(using0, using1);
            AppendLine(code);
        }

        public void AppendLine(string code, string using0, string using1, string using2)
        {
            AddUsings(using0, using1, using2);
            AppendLine(code);
        }

        public void AppendLine(string code, params string[] usings)
        {
            AddUsings(usings);
            AppendLine(code);
        }

        public void AppendLineNoIndent(string code)
        {
            _ = stringBuilder.AppendLine(code);
        }

        public void Reset()
        {
            _ = stringBuilder.Clear();
            usings.Clear();
            indentLevel = 0;
        }

        public string Build()
        {
            try
            {
                InsertUsings();
                return stringBuilder.ToString();
            }
            finally
            {
                Reset();
            }
        }

        private void InsertUsings()
        {
            if (usings.Count != 0)
            {
                // Extra line after usings
                _ = stringBuilder.Insert(0, Environment.NewLine);
            }

            foreach (string @using in usings.Reverse())
            {
                _ = stringBuilder.Insert(0, $"using {@using};{Environment.NewLine}");
            }
        }

        public readonly struct PragmaDisableArea : IDisposable
        {
            private readonly CodeBuilder? owner;
            private readonly string pragma;

            internal PragmaDisableArea(CodeBuilder owner, string pragma)
            {
                this.owner = owner;
                this.pragma = pragma;

                owner.AppendLineNoIndent($"#pragma warning disable {pragma}");
            }

            public void Dispose()
            {
                if (owner != null && owner.IndentLevel > 0)
                {
                    owner.AppendLineNoIndent($"#pragma warning restore {pragma}");
                }
            }
        }

        public readonly struct IndentedArea : IDisposable
        {
            private readonly CodeBuilder? owner;

            internal IndentedArea(CodeBuilder owner)
            {
                this.owner = owner;
                ++owner.IndentLevel;
            }

            public void Dispose()
            {
                if (owner != null && owner.IndentLevel > 0)
                {
                    --owner.IndentLevel;
                }
            }
        }

        public readonly struct CurlyBracesArea : IDisposable
        {
            private readonly CodeBuilder? owner;

            internal CurlyBracesArea(CodeBuilder owner)
            {
                this.owner = owner;

                owner.AppendLine("{");
                ++owner.IndentLevel;
            }

            public void Dispose()
            {
                if (owner != null && owner.IndentLevel > 0)
                {
                    --owner.IndentLevel;
                    owner.AppendLine("}");
                }
            }
        }

        public readonly struct CurlyBracesWithSemicolonArea : IDisposable
        {
            private readonly CodeBuilder? owner;

            internal CurlyBracesWithSemicolonArea(CodeBuilder owner)
            {
                this.owner = owner;

                owner.AppendLine("{");
                ++owner.IndentLevel;
            }

            public void Dispose()
            {
                if (owner != null && owner.IndentLevel > 0)
                {
                    --owner.IndentLevel;
                    owner.AppendLine("};");
                }
            }
        }

        public readonly struct UnsafeArea : IDisposable
        {
            private readonly CodeBuilder? owner;

            internal UnsafeArea(CodeBuilder owner)
            {
                this.owner = owner;

                owner.AppendLine("unsafe");
                owner.AppendLine("{");
                ++owner.IndentLevel;
            }

            public void Dispose()
            {
                if (owner != null && owner.IndentLevel > 0)
                {
                    --owner.IndentLevel;
                    owner.AppendLine("}");
                }
            }
        }

        public struct GenericTypeParametersArea : IDisposable
        {
            private readonly CodeBuilder? owner;
            private bool needsComma;

            internal GenericTypeParametersArea(CodeBuilder owner)
            {
                this.owner = owner;
                owner.Append('<');
            }

            public void AppendComma()
            {
                owner!.Append(", ");
                needsComma = true;
            }

            public void AppendParameter(string parameter)
            {
                if (needsComma)
                {
                    AppendComma();
                }

                owner!.Append(parameter);
                needsComma = true;
            }

            public readonly void Dispose()
            {
                owner?.Append('>');
            }
        }

        public readonly struct TypeDeclarationArea : IDisposable
        {
            private readonly CodeBuilder? owner;
            private readonly int nestingDepth;

            internal TypeDeclarationArea(CodeBuilder owner, INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsGenericType)
                {
                    throw new NotSupportedException("Generic types not supported.");
                }

                this.owner = owner;

                INamedTypeSymbol[] typeHierarchy = SymbolUtility.GetTypeHierarchy(namedTypeSymbol);
                nestingDepth = typeHierarchy.Length;

                foreach (INamedTypeSymbol hierarchyTypeSymbol in typeHierarchy)
                {
                    owner.AppendLine($"{SymbolUtility.GetDeclarationPrefix(hierarchyTypeSymbol)} {hierarchyTypeSymbol.Name}");
                    owner.AppendLine("{");
                    ++owner.IndentLevel;
                }
            }

            public readonly void Dispose()
            {
                if (owner != null)
                {
                    for (int index = 0; index != nestingDepth; ++index)
                    {
                        --owner.IndentLevel;
                        owner.AppendLine("}");
                    }
                }
            }
        }
    }
}
