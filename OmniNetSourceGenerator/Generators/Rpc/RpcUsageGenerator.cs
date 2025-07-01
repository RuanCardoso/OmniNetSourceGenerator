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
            if (!GenHelper.WillProcess(context.Compilation.Assembly))
                return;

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
                                bool isNetworkBehaviour = fromClass.InheritsFromClass(classModel, "NetworkBehaviour");
                                bool isClientBehaviour = fromClass.InheritsFromClass(classModel, "ClientBehaviour");
                                bool isServerBehaviour = fromClass.InheritsFromClass(classModel, "ServerBehaviour");
                                bool isDualBehaviour = fromClass.InheritsFromClass(classModel, "DualBehaviour");
                                NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
                                if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

                                List<MemberDeclarationSyntax> memberList = new List<MemberDeclarationSyntax>();
                                List<MemberDeclarationSyntax> relatedMemberList = new List<MemberDeclarationSyntax>();

                                // Create dummy method that calls all RPCs
                                var methodBody = new StringBuilder();
                                methodBody.AppendLine("if (false) {"); // Ensure the code is never executed

                                string classname = fromClass.GetAttribute("GenRpc")?.GetArgumentValue<string>("classname", ArgumentIndex.First, classModel, null) ?? null;
                                foreach (MethodDeclarationSyntax method in @class.Members.Cast<MethodDeclarationSyntax>())
                                {
                                    bool isClientRpc = method.HasAttribute("Client");
                                    bool isServerRpc = method.HasAttribute("Server");

                                    if (!isClientRpc && !isServerRpc)
                                        continue;

                                    string methodName = method.Identifier.Text;
                                    var parameters = method.ParameterList.Parameters;
                                    methodBody.AppendLine($"{methodName}({string.Join(", ", Enumerable.Repeat("default", parameters.Count))});");
                                    var attribute = method.GetAttribute(isClientRpc ? "Client" : "Server");
                                    if (!GenHelper.IsManualRpc(method))
                                    {
                                        memberList.Add(GenerateRpcMethod(method, attribute));
                                        bool requiresPeer = isDualBehaviour || isClientBehaviour || isServerBehaviour;
                                        var member = GenerateDirectRpcMethod(method, attribute.GetArgumentValue<byte>("id", ArgumentIndex.First, classModel), isServerRpc, requiresPeer: requiresPeer);
                                        if (classname != null) relatedMemberList.Add(member);
                                        else memberList.Add(member);
                                    }
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

                                ClassDeclarationSyntax relatedClass = null;
                                if (classname != null)
                                {
                                    relatedClass = SyntaxFactory.ClassDeclaration(classname).WithModifiers(fromClass.Modifiers);
                                    relatedClass = relatedClass.AddMembers(relatedMemberList.ToArray());
                                }

                                if (!hasNamespace)
                                {
                                    sb.AppendLine();
                                    sb.AppendLine("// Generated by OmniNetSourceGenerator");
                                    if (relatedClass != null)
                                    {
                                        sb.Append(relatedClass.NormalizeWhitespace().ToString());
                                        sb.AppendLine();
                                        sb.AppendLine();
                                    }

                                    sb.Append(parentClass.NormalizeWhitespace().ToString());
                                }
                                else
                                {
                                    if (relatedClass != null)
                                    {
                                        currentNamespace = currentNamespace.AddMembers(relatedClass);
                                    }

                                    currentNamespace = currentNamespace.AddMembers(parentClass);
                                    sb.AppendLine("// Generated by OmniNetSourceGenerator");
                                    sb.Append(currentNamespace.NormalizeWhitespace().ToString());
                                }

                                context.AddSource($"{parentClass.Identifier.Text}_rpc_usage_generated_code_.cs", sb.ToString());
                            }
                            else
                            {
                                GenHelper.ReportPartialKeywordRequirement(new Context(context), fromClass);
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

        private MethodDeclarationSyntax GenerateDirectRpcMethod(MethodDeclarationSyntax method, byte rpcId, bool isServerRpc, bool requiresPeer)
        {
            var originalParams = method.ParameterList.Parameters
                .Where(p =>
                    p.Type.ToString() != "NetworkPeer" &&
                    p.Type.ToString() != "Channel"
                )
                .ToList();

            var extraParams = new List<ParameterSyntax>();

            if (requiresPeer && !isServerRpc)
            {
                extraParams.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("peer"))
                        .WithType(SyntaxFactory.ParseTypeName("NetworkPeer"))
                );
            }

            if (!isServerRpc)
            {
                extraParams.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("group"))
                        .WithType(SyntaxFactory.ParseTypeName("NetworkGroup"))
                        .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))
                );
                extraParams.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("target"))
                        .WithType(SyntaxFactory.ParseTypeName("Target"))
                        .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("Target.Auto")))
                );
            }

            extraParams.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("mode"))
                    .WithType(SyntaxFactory.ParseTypeName("DeliveryMode"))
                    .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("DeliveryMode.ReliableOrdered")))
            );
            extraParams.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("channel"))
                    .WithType(SyntaxFactory.ParseTypeName("int"))
                    .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))))
            );

            if (isServerRpc)
                extraParams = extraParams.Where(p => p.Identifier.Text != "group" && p.Identifier.Text != "target").ToList();

            var allParams = originalParams.Concat(extraParams);

            var methodName = $"{method.Identifier.Text}Rpc";

            // Create argument list for the RPC call
            var arguments = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(rpcId)))
            };

            if (requiresPeer && !isServerRpc)
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("peer")));

            arguments.AddRange(originalParams.Select(p =>
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier.Text))));

            if (!isServerRpc)
            {
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group: group")));
            }

            var rpcCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(isServerRpc ? "Client" : "Server"),
                        SyntaxFactory.IdentifierName("Rpc")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))
                )
            );

            var CallRpcParameters = !isServerRpc ? SyntaxFactory.ParseStatement($"Server.SetRpcParameters({rpcId}, mode, target, channel);") : SyntaxFactory.ParseStatement($"Client.SetRpcParameters({rpcId}, mode, channel);");
            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    methodName
                )
                .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) }))
                .WithParameterList(
                    SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(allParams))
                )
                .WithBody(SyntaxFactory.Block(new[] { CallRpcParameters, rpcCall }));
        }

        private MethodDeclarationSyntax GenerateRpcMethod(MethodDeclarationSyntax method, AttributeSyntax attribute)
        {
            var statements = new List<StatementSyntax>();
            var methodParameters = method.ParameterList.Parameters;
            var methodArguments = new List<ArgumentSyntax>();

            for (int i = 0; i < methodParameters.Count; i++)
            {
                var parameter = methodParameters[i];
                var paramType = parameter.Type.ToString();
                var paramName = $"var{i + 1}";

                string statement = $"{paramType} {paramName} = message.ReadAsBinary<{paramType}>();";
                if (paramType == "NetworkPeer") statement = $"{paramType} {paramName} = peer;";
                else if (paramType == "Channel") statement = $"{paramType} {paramName} = seqChannel;";

                statements.Add(SyntaxFactory.ParseStatement(statement));
                methodArguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(paramName)));
            }

            statements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(method.Identifier.ToString()),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(methodArguments))
                )
            ));

            return SyntaxFactory.MethodDeclaration(method.ReturnType, SyntaxFactory.Identifier("__" + method.Identifier.Text))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithAttributeLists(method.AttributeLists)
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    attribute.Name.ToString() == "Server"
                        ? new[] {
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                                    .WithType(SyntaxFactory.ParseTypeName("DataBuffer")),
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("peer"))
                                    .WithType(SyntaxFactory.ParseTypeName("NetworkPeer")),
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("seqChannel"))
                                    .WithType(SyntaxFactory.ParseTypeName("int"))
                        }
                        : new[] {
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                                    .WithType(SyntaxFactory.ParseTypeName("DataBuffer")),
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("seqChannel"))
                                    .WithType(SyntaxFactory.ParseTypeName("int"))
                        }
                )
            ))
            .WithBody(SyntaxFactory.Block(statements));
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