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
    public class RpcSignatureAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor InvalidRpcSignature = new DiagnosticDescriptor(
            id: "OMNI033",
            title: "Invalid RPC Method Signature",
            messageFormat: "The {0} RPC method '{1}' has an invalid signature. Valid signatures are: {2}",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "RPC methods must follow specific signature patterns to ensure proper network communication."
        );

        private const string ClientValidSignatures =
            "\r\n\r\nvoid Method()\r\n" +
            "void Method(DataBuffer message)\r\n" +
            "void Method(DataBuffer message, int seqChannel)\r\n";

        private const string ServerValidSignatures =
            "\r\n\r\nvoid Method()\r\n" +
            "void Method(DataBuffer message)\r\n" +
            "void Method(DataBuffer message, NetworkPeer peer)\r\n" +
            "void Method(DataBuffer message, NetworkPeer peer, int seqChannel)\r\n";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(InvalidRpcSignature);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (!GenHelper.WillProcess(context.Compilation.Assembly))
                return;

            if (context.Node is MethodDeclarationSyntax method)
            {
                bool isClientRpc = method.HasAttribute("Client");
                bool isServerRpc = method.HasAttribute("Server");

                if (!isClientRpc && !isServerRpc)
                    return;

                if (!IsValidSignature(method, context.SemanticModel, isClientRpc))
                {
                    string rpcType = isClientRpc ? "Client" : "Server";
                    string validSignatures = isClientRpc ? ClientValidSignatures : ServerValidSignatures;

                    bool isAutoRpc = !GenHelper.IsManualRpc(method);
                    if (isAutoRpc)
                        return;

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            InvalidRpcSignature,
                            method.Identifier.GetLocation(),
                            rpcType,
                            method.Identifier.Text,
                            validSignatures
                        )
                    );
                }
            }
        }

        private bool IsValidSignature(MethodDeclarationSyntax method, SemanticModel semanticModel, bool isClientRpc)
        {
            // Check return type is void
            if (method.ReturnType.ToString() != "void")
                return false;

            var parameters = method.ParameterList.Parameters;
            int paramCount = parameters.Count;

            if (isClientRpc)
            {
                switch (paramCount)
                {
                    case 0:
                        return true; // void Method()
                    case 1:
                        return IsParameterOfType(parameters[0], "DataBuffer", semanticModel);
                    case 2:
                        return IsParameterOfType(parameters[0], "DataBuffer", semanticModel) &&
                               IsParameterOfType(parameters[1], "int", semanticModel);
                    default:
                        return false;
                }
            }
            else // Server RPC
            {
                switch (paramCount)
                {
                    case 0:
                        return true; // void Method()
                    case 1:
                        return IsParameterOfType(parameters[0], "DataBuffer", semanticModel);
                    case 2:
                        return IsParameterOfType(parameters[0], "DataBuffer", semanticModel) &&
                               IsParameterOfType(parameters[1], "NetworkPeer", semanticModel);
                    case 3:
                        return IsParameterOfType(parameters[0], "DataBuffer", semanticModel) &&
                               IsParameterOfType(parameters[1], "NetworkPeer", semanticModel) &&
                               IsParameterOfType(parameters[2], "int", semanticModel);
                    default:
                        return false;
                }
            }
        }

        private bool IsParameterOfType(ParameterSyntax parameter, string typeName, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(parameter.Type);
            return typeInfo.Type?.ToString().EndsWith(typeName) ?? false;
        }
    }
}