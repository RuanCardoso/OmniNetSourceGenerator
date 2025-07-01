using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using System.Collections.Immutable;
using System.Linq;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ModelAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor ModelFieldError = new DiagnosticDescriptor(
            id: "OMNI091",
            title: "Model Class Field Not Allowed",
            messageFormat: "Field '{0}' is not allowed in a [Model] class or struct. Use properties with getters and setters instead for proper serialization.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Classes or structs with the [Model] attribute must use properties instead of fields for better serialization and validation support."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(ModelFieldError);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register syntax node actions for class and struct declarations
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.StructDeclaration);
        }

        private void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is TypeDeclarationSyntax typeDecl))
                return;

            if (!typeDecl.HasAttribute("Model"))
                return;

            foreach (var member in typeDecl.Members)
            {
                if (member is FieldDeclarationSyntax fieldDecl)
                {
                    if (fieldDecl.HasModifier(SyntaxKind.ConstKeyword))
                        continue;

                    if (fieldDecl.HasModifier(SyntaxKind.StaticKeyword))
                        continue;

                    foreach (var variable in fieldDecl.Declaration.Variables)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                ModelFieldError,
                                variable.GetLocation(),
                                variable.Identifier.Text
                            )
                        );
                    }
                }
            }
        }
    }
}
