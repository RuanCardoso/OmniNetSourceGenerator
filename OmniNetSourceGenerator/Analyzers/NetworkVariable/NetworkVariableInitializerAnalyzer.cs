using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkVariableInitializerAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor UninitializedReferenceTypeWarning = new DiagnosticDescriptor(
            id: "OMNI028",
            title: "Design: Uninitialized Network Variable",
            messageFormat: "The Network Variable '{0}' is a reference type but has no initialization or assignment in the code. This could lead to null reference exceptions. Consider initializing the variable with a default value.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reference type Network Variables should be initialized to prevent null reference exceptions:" +
                        "\n1. Variables could be null when network synchronization starts" +
                        "\n2. Null checks would be required throughout the code" +
                        "\n3. May cause runtime errors if accessed before assignment" +
                        "\n\nRecommended actions:" +
                        "\n- Initialize the variable with a default value" +
                        "\n- Ensure the variable is assigned in Start() or Awake()" +
                        "\n- Add null checks where the variable is used"
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[]
        {
            UninitializedReferenceTypeWarning
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptors);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is FieldDeclarationSyntax field)
            {
                if (field.HasAttribute("NetworkVariable"))
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(field.Declaration.Type);
                    if (typeInfo.Type != null && !typeInfo.Type.IsValueType)
                    {
                        bool isSerializable = typeInfo.Type.HasAttribute("SerializableAttribute");
                        if (isSerializable)
                        {
                            bool isSerializedField = field.HasAttribute("SerializeField");
                            if (isSerializedField)
                            {
                                return;
                            }

                            if (field.HasModifier(SyntaxKind.PublicKeyword))
                            {
                                return;
                            }
                        }

                        var classDeclaration = field.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                        if (classDeclaration != null)
                        {
                            foreach (var variable in field.Declaration.Variables)
                            {
                                bool hasInitializer = variable.Initializer != null;
                                if (!hasInitializer)
                                {
                                    var assignments = FindFieldAssignments(context, classDeclaration, variable.Identifier.Text);
                                    if (!assignments.Any())
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                UninitializedReferenceTypeWarning,
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
            }
        }

        private IEnumerable<AssignmentExpressionSyntax> FindFieldAssignments(
            SyntaxNodeAnalysisContext context,
            ClassDeclarationSyntax classDeclaration,
            string fieldName)
        {
            return classDeclaration.GetDescendantsOfType<AssignmentExpressionSyntax>()
                .Where(assignment =>
                {
                    if (assignment.Left is IdentifierNameSyntax identifier)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                        return symbol != null &&
                               symbol.Kind == SymbolKind.Field &&
                               symbol.Name == fieldName;
                    }

                    return false;
                });
        }
    }
}