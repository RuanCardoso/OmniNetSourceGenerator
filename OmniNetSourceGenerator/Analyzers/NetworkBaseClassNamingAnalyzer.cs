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
    public class NetworkBaseClassNamingAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor BaseClassNamingSuggestion = new DiagnosticDescriptor(
            id: "OMNI053",
            title: "Network Base Class Naming Convention",
            messageFormat: "Base class '{0}' should end with 'Base' suffix to follow naming conventions",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Network base classes should follow the naming convention of ending with 'Base' " +
                         "to clearly identify them as base classes and distinguish them from concrete implementations. " +
                         "For example, use 'PlayerBase' instead of 'Player' for a base class that will be inherited from."
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[] {
            BaseClassNamingSuggestion
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptors);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register a syntax node action for class declarations
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classDeclaration))
                return;

            // Skip if the class already follows the naming convention
            string className = classDeclaration.Identifier.Text;
            if (className.EndsWith("Base"))
                return;

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol == null)
                return;

            // Check if this is a network-related class
            bool isNetworkRelated = classDeclaration.InheritsFromClass(semanticModel,
                "NetworkBehaviour", "DualBehaviour", "ClientBehaviour", "ServerBehaviour");

            if (!isNetworkRelated)
                return;

            // Only check for abstract classes or classes that are inherited from
            bool isAbstract = classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);
            if (!isAbstract)
            {
                // Check if there are references to this class as a base type
                if (!HasDerivedClasses(classSymbol, context.Compilation))
                    return;
            }

            // Report diagnostic for network-related base classes without 'Base' suffix
            context.ReportDiagnostic(
                Diagnostic.Create(
                    BaseClassNamingSuggestion,
                    classDeclaration.Identifier.GetLocation(),
                    className
                )
            );
        }

        private bool HasDerivedClasses(INamedTypeSymbol classSymbol, Compilation compilation)
        {
            // Find references to this type
            var references = compilation.GetSymbolsWithName(
                name => true, // Get all symbols
                SymbolFilter.Type
            );

            foreach (var symbol in references)
            {
                if (symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.BaseType != null &&
                    SymbolEqualityComparer.Default.Equals(typeSymbol.BaseType, classSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}