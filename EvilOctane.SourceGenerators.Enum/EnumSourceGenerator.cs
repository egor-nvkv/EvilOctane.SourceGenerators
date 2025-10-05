using EvilOctane.SourceGenerators.Code;
using EvilOctane.SourceGenerators.Type;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace EvilOctane.SourceGenerators.Enum
{
    [Generator]
    public class EnumSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EnumSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            TypeNameDictionary typeNameDictionary = TypeNameDictionary.Create();
            CodeBuilder codeBuilder = new();

            if (context.SyntaxReceiver is not EnumSyntaxReceiver enumSyntaxReceiver)
            {
                return;
            }

            try
            {
                string code = "";
                context.AddSource("EnumDummyCodeGen.gen.cs", SourceText.From(code, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                DiagnosticDescriptor diagnosticDescriptor = new(
                    id: "exception",
                    title: "Exception in source generator",
                    messageFormat: "Exception was thrown by {0} generator: {1}",
                    category: nameof(EnumSourceGenerator),
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                Diagnostic diagnostic = Diagnostic.Create(
                    diagnosticDescriptor,
                    Location.None,
                    nameof(EnumSourceGenerator),
                    $"{exception}{Environment.NewLine}{exception.StackTrace}");

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
