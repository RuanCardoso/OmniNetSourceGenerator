using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RpcDuplicateIdAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DuplicateRpcId = new DiagnosticDescriptor(
            id: "OMNI041",
            title: "Duplicate RPC ID",
            messageFormat: "The {0} RPC method with Id {1} is already defined in {2}",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "RPC methods must have unique IDs within their type (Server/Client) in the inheritance hierarchy."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DuplicateRpcId);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            if (!GenHelper.WillProcess(context.Compilation.Assembly))
            {
                return;
            }

            if (context.Node is ClassDeclarationSyntax @class)
            {
                var semanticModel = context.SemanticModel;
                var classSymbol = semanticModel.GetDeclaredSymbol(@class);
                if (classSymbol == null)
                    return;

                var serverRpcs = new Dictionary<byte, (string MethodName, Location Location, string ClassName)>();
                var clientRpcs = new Dictionary<byte, (string MethodName, Location Location, string ClassName)>();
                CollectRpcMethods(new Context(context), classSymbol, serverRpcs, clientRpcs, semanticModel);
            }
        }

        private void CollectRpcMethods(
            Context context,
            INamedTypeSymbol classSymbol,
            Dictionary<byte, (string methodName, Location location, string className)> serverRpcs,
            Dictionary<byte, (string methodName, Location location, string className)> clientRpcs,
            SemanticModel semanticModel)
        {
            if (classSymbol.BaseType != null)
                CollectRpcMethods(context, classSymbol.BaseType, serverRpcs, clientRpcs, semanticModel);

            if (!(classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax syntax))
                return;

            foreach (var member in syntax.Members)
            {
                if (member is MethodDeclarationSyntax method)
                {
                    if (method.HasAttribute("Server"))
                    {
                        ProcessRpcMethod(method, "Server", serverRpcs, classSymbol, context);
                    }
                    else if (method.HasAttribute("Client"))
                    {
                        ProcessRpcMethod(method, "Client", clientRpcs, classSymbol, context);
                    }
                }
            }
        }

        private void ProcessRpcMethod(
            MethodDeclarationSyntax method,
            string rpcType,
            Dictionary<byte, (string methodName, Location location, string className)> rpcs,
            INamedTypeSymbol classSymbol,
            Context context)
        {
            if (!GetRpcId(method, rpcType, context.SyntaxNodeAnalysisContext.Value.SemanticModel, out byte currentId))
                return;

            string methodName = method.Identifier.Text;
            if (rpcs.TryGetValue(currentId, out var existing))
            {
                context.ReportDiagnostic(
                    DuplicateRpcId,
                    method.Identifier.GetLocation(),
                    rpcType,
                    currentId.ToString(),
                    $"{existing.className}.{existing.methodName}");

                return;
            }

            rpcs[currentId] = (methodName, method.Identifier.GetLocation(), classSymbol.Name);
        }

        private bool GetRpcId(MethodDeclarationSyntax method, string attributeName, SemanticModel semanticModel, out byte id)
        {
            AttributeSyntax attribute = method.GetAttribute(attributeName);
            if (attribute != null)
            {
                id = attribute.GetArgumentValue<byte>("id", ArgumentIndex.First, semanticModel, 0);
                return id > 0;
            }

            id = 0;
            return false;
        }
    }
}