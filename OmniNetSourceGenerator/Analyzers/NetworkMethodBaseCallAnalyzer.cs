using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System.Collections.Immutable;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkMethodBaseCallAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor MissingBaseAwakeCall = new DiagnosticDescriptor(
            id: "OMNI051",
            title: "Missing base.Awake() Call",
            messageFormat: "The Awake method in '{0}' must call base.Awake() to ensure proper network initialization. Consider using OnAwake() instead of overriding Awake().",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Network behaviors that override the Awake method must call base.Awake() to ensure " +
                         "proper initialization of network components and systems. Missing this call can lead " +
                         "to synchronization issues, connection problems, and other network-related bugs."
        );

        public static readonly DiagnosticDescriptor MissingBaseStartCall = new DiagnosticDescriptor(
            id: "OMNI052",
            title: "Missing base.Start() Call",
            messageFormat: "The Start method in '{0}' should call base.Start() to ensure proper network component initialization. Consider using OnStart() instead of overriding Start().",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Network behaviors that override the Start method should call base.Start() to ensure " +
                         "proper initialization of network components. Missing this call can lead to components " +
                         "not being initialized correctly, which may cause synchronization issues."
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[] {
            MissingBaseAwakeCall,
            MissingBaseStartCall
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptors);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!GenHelper.WillProcess(context.Compilation.Assembly))
            {
                return;
            }

            if (context.Node is MethodDeclarationSyntax method)
            {
                // Check if the method is Awake or Start
                string methodName = method.Identifier.Text;
                if (methodName != "Awake" && methodName != "Start")
                    return;

                // Check if this is in a class that inherits from a network behavior
                if (method.Parent is ClassDeclarationSyntax classDeclaration)
                {
                    var model = context.SemanticModel;
                    bool isDualBehaviour = classDeclaration.InheritsFromClass(model, "DualBehaviour");
                    bool isClientBehaviour = classDeclaration.InheritsFromClass(model, "ClientBehaviour");
                    bool isServerBehaviour = classDeclaration.InheritsFromClass(model, "ServerBehaviour");

                    if (!isDualBehaviour && !isClientBehaviour && !isServerBehaviour)
                        return;

                    // Check if the method has a body
                    if (method.Body == null)
                        return;

                    // Look for a base.Awake() or base.Start() call
                    bool hasBaseCall = false;
                    foreach (var statement in method.Body.Statements)
                    {
                        if (statement is ExpressionStatementSyntax expressionStatement &&
                            expressionStatement.Expression is InvocationExpressionSyntax invocation &&
                            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            memberAccess.Expression is BaseExpressionSyntax &&
                            memberAccess.Name.Identifier.Text == methodName)
                        {
                            hasBaseCall = true;
                            break;
                        }
                    }

                    if (!hasBaseCall)
                    {
                        // Report missing base call
                        DiagnosticDescriptor descriptor = methodName == "Awake" ? MissingBaseAwakeCall : MissingBaseStartCall;
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                descriptor,
                                method.Identifier.GetLocation(),
                                classDeclaration.Identifier.Text
                            )
                        );
                    }
                }
            }
        }
    }
}