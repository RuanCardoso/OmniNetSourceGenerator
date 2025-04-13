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
    public class DataBufferRentAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DataBufferRentSuggestion = new DiagnosticDescriptor(
            id: "OMNI060",
            title: "Use Rent() Instead of new",
            messageFormat: "Consider using Rent() instead of creating a new DataBuffer instance for better performance",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "In network classes, using Rent() instead of the constructor allows for buffer pooling, " +
                         "which can significantly improve performance by reducing memory allocations and garbage collection pressure."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DataBufferRentSuggestion);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register a syntax node action for object creation expressions
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

            // Register for implicit object creation expressions (new() syntax)
            context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ObjectCreationExpressionSyntax objectCreation))
                return;

            // Get semantic model and type information
            var semanticModel = context.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(objectCreation);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null || typeSymbol.Name != "DataBuffer")
                return;

            // Check if we're inside a network class (inherits from a network base class)
            if (!IsInsideNetworkClass(objectCreation, semanticModel))
                return;

            // Report diagnostic for creating a new DataBuffer directly in a network class
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DataBufferRentSuggestion,
                    objectCreation.GetLocation()
                )
            );
        }

        private void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ImplicitObjectCreationExpressionSyntax implicitObjectCreation))
                return;

            // Get semantic model and type information
            var semanticModel = context.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(implicitObjectCreation);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null || typeSymbol.Name != "DataBuffer")
                return;

            // Check if we're inside a network class (inherits from a network base class)
            if (!IsInsideNetworkClass(implicitObjectCreation, semanticModel))
                return;

            // Report diagnostic for creating a new DataBuffer with implicit new() in a network class
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DataBufferRentSuggestion,
                    implicitObjectCreation.GetLocation()
                )
            );
        }

        private bool IsInsideNetworkClass(SyntaxNode node, SemanticModel semanticModel)
        {
            // Find the containing class declaration
            var containingClass = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (containingClass == null)
                return false;

            // Check if this class inherits from one of the network-related base classes
            return containingClass.InheritsFromClass(semanticModel,
                "NetworkBehaviour", "DualBehaviour", "ClientBehaviour", "ServerBehaviour");
        }
    }
}