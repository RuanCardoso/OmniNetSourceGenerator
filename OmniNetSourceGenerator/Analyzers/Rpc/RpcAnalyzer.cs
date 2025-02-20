using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RpcAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor RpcMethodShouldBePrivate = new DiagnosticDescriptor(
            id: "OMNI031",
            title: "RPC Method Should Be Private",
            messageFormat: "The RPC method '{0}' should be private by design.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor StaticRpcMethod = new DiagnosticDescriptor(
            id: "OMNI032",
            title: "RPC Method Should Not Be Static",
            messageFormat: "The RPC method '{0}' should not be static.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor RpcMethodNamingConvention = new DiagnosticDescriptor(
            id: "OMNI034",
            title: "RPC Method Naming Convention",
            messageFormat: "The RPC method '{0}' should end with 'Rpc' suffix for better code readability",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Following a consistent naming convention helps identify RPC methods at a glance and improves code maintainability."
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[]
        {
            RpcMethodShouldBePrivate,
            GenHelper.InheritanceConstraintViolation,
            StaticRpcMethod,
            GenHelper.PartialKeywordMissing,
            RpcMethodNamingConvention,
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
            if (context.Node is MethodDeclarationSyntax method)
            {
                if (method.HasAttribute("Server") || method.HasAttribute("Client"))
                {
                    Context cContext = new Context(context);
                    if (method.HasModifier(SyntaxKind.StaticKeyword))
                    {
                        cContext.ReportDiagnostic(
                            StaticRpcMethod,
                            method.Identifier.GetLocation(),
                            method.Identifier.Text
                        );
                    }

                    if (!method.Identifier.Text.EndsWith("Rpc", StringComparison.Ordinal))
                    {
                        cContext.ReportDiagnostic(
                            RpcMethodNamingConvention,
                            method.Identifier.GetLocation(),
                            method.Identifier.Text
                        );
                    }

                    if (!method.HasModifier(SyntaxKind.PrivateKeyword))
                    {
                        cContext.ReportDiagnostic(
                            RpcMethodShouldBePrivate,
                            method.Identifier.GetLocation(),
                            method.Identifier.Text
                        );
                    }

                    if (method.Parent is ClassDeclarationSyntax @class)
                    {
                        GenHelper.ReportPartialKeywordRequirement(cContext, @class, method.Identifier.GetLocation());

                        bool isNetworkBehaviour = @class.InheritsFromClass(context.SemanticModel, "NetworkBehaviour");
                        bool isClientBehaviour = @class.InheritsFromClass(context.SemanticModel, "ClientBehaviour");
                        bool isServerBehaviour = @class.InheritsFromClass(context.SemanticModel, "ServerBehaviour");
                        bool isDualBehaviour = @class.InheritsFromClass(context.SemanticModel, "DualBehaviour");

                        if (!isNetworkBehaviour && !isClientBehaviour && !isServerBehaviour && !isDualBehaviour)
                        {
                            GenHelper.ReportInheritanceRequirement(cContext, @class.Identifier.Text, method.Identifier.GetLocation());
                        }
                    }
                }
            }
        }
    }
}