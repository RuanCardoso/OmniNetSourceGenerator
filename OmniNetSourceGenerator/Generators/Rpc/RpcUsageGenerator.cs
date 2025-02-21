using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniNetSourceGenerator
{
    [Generator]
    internal class RpcUsageGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.SyntaxReceiver is RpcMethodSyntaxReceiver receiver)
                {
                    if (receiver.methods.Any())
                    {
                        var classes = receiver.methods.GroupByDeclaringClass();
                        foreach (ClassStructure @class in classes)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("#nullable disable");
                            sb.AppendLine("#pragma warning disable");
                            sb.AppendLine("using UnityEngine.Scripting;");
                            sb.AppendLine("using System.ComponentModel;");
                            sb.AppendLine();

                            ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
                            foreach (var usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
                                sb.AppendLine(usingSyntax.ToString());

                            if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
                            {
                                var classModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
                                NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
                                if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

                                List<MemberDeclarationSyntax> memberList = new List<MemberDeclarationSyntax>();

                                // Create dummy method that calls all RPCs
                                var methodBody = new StringBuilder();
                                methodBody.AppendLine("if (false) {"); // Ensure the code is never executed

                                foreach (MethodDeclarationSyntax method in @class.Members.Cast<MethodDeclarationSyntax>())
                                {
                                    bool isClientRpc = method.HasAttribute("Client");
                                    bool isServerRpc = method.HasAttribute("Server");

                                    if (!isClientRpc && !isServerRpc) continue;

                                    string methodName = method.Identifier.Text;
                                    var parameters = method.ParameterList.Parameters;

                                    // Generate dummy parameter values
                                    var args = new List<string>();
                                    foreach (var param in parameters)
                                    {
                                        if (param.Type.ToString() == "DataBuffer")
                                            args.Add("null");
                                        else if (param.Type.ToString() == "NetworkPeer")
                                            args.Add("null");
                                        else if (param.Type.ToString() == "int")
                                            args.Add("0");
                                    }

                                    methodBody.AppendLine($"{methodName}({string.Join(", ", args)});");
                                }

                                methodBody.AppendLine("}");

                                // Create the dummy method
                                memberList.Add(
                                    SyntaxFactory.MethodDeclaration(
                                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                        "PreventRpcStripping"
                                    )
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            new[]{
                                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                            }
                                        )
                                    )
                                    .WithAttributeLists(
                                        SyntaxFactory.SingletonList(
                                            SyntaxFactory.AttributeList(
                                               SyntaxFactory.SeparatedList(
                                                    new AttributeSyntax[] {
                                                       SyntaxFactory.Attribute(SyntaxFactory.ParseName("Preserve")),
                                                       SyntaxFactory.Attribute(SyntaxFactory.ParseName("EditorBrowsable"), SyntaxFactory.ParseAttributeArgumentList("(EditorBrowsableState.Never)"))
                                                  }
                                               )
                                            )
                                        )
                                    )
                                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(methodBody.ToString())))
                                );

                                parentClass = parentClass.AddMembers(memberList.ToArray());

                                if (!hasNamespace)
                                {
                                    sb.AppendLine("// Generated by OmniNetSourceGenerator");
                                    sb.Append(parentClass.NormalizeWhitespace().ToString());
                                }
                                else
                                {
                                    currentNamespace = currentNamespace.AddMembers(parentClass);
                                    sb.AppendLine("// Generated by OmniNetSourceGenerator");
                                    sb.Append(currentNamespace.NormalizeWhitespace().ToString());
                                }

                                context.AddSource($"{parentClass.Identifier.Text}_rpc_usage_generated_code_.cs", sb.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "OMNI000",
                            "Unhandled Exception Detected",
                            $"An unhandled exception occurred: {ex.Message}. Stack Trace: {ex.StackTrace}",
                            "Runtime",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true
                        ),
                        Location.None
                    )
                );
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new RpcMethodSyntaxReceiver());
        }
    }

    internal class RpcMethodSyntaxReceiver : ISyntaxReceiver
    {
        internal List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax methodSyntax)
            {
                if (methodSyntax.HasAttribute("Client", "Server"))
                {
                    methods.Add(methodSyntax);
                }
            }
        }
    }
}