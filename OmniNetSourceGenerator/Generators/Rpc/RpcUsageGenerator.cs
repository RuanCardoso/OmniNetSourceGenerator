using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace OmniNetSourceGenerator
{
    [Generator]
    internal class RpcUsageGenerator : ISourceGenerator
    {
        private static Dictionary<string, byte> GetCollectionOfUniqueIds(ITypeSymbol symbol, string attrName)
        {
            var result = new Dictionary<string, byte>();
            byte index = 1;

            // 1. Sobe toda a cadeia até a raiz (System.Object normalmente)
            var stack = new Stack<ITypeSymbol>();
            var current = symbol;
            while (current != null)
            {
                if (current.TypeKind == TypeKind.Class)
                    stack.Push(current);

                current = current.BaseType;
            }

            // 2. Desce da base até o filho
            while (stack.Count > 0)
            {
                var type = stack.Pop();

                foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (member.MethodKind == MethodKind.Ordinary)
                    {
                        var hasAttr = member
                            .GetAttributes()
                            .Any(a => a.AttributeClass?.Name == attrName
                                   || a.AttributeClass?.ToDisplayString() == attrName);

                        if (hasAttr)
                        {
                            result[member.Name] = index;
                            index++;
                        }
                    }
                }
            }

            return result;
        }

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
                            sb.AppendLine("using System;");
                            sb.AppendLine("using UnityEngine.Scripting;");
                            sb.AppendLine("using System.ComponentModel;");
                            sb.AppendLine();

                            ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
                            foreach (var usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
                                sb.AppendLine(usingSyntax.ToString());

                            if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
                            {
                                var classModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
                                var uniqueClientIds = GetCollectionOfUniqueIds(classModel.GetDeclaredSymbol(fromClass), "ClientAttribute");
                                var uniqueServerIds = GetCollectionOfUniqueIds(classModel.GetDeclaredSymbol(fromClass), "ServerAttribute");

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

                                    // Generate auto id to the RPC if not specified..
                                    AttributeSyntax attribute = method.GetAttribute(isClientRpc ? "Client" : "Server");
                                    byte currentId = attribute.GetArgumentValue<byte>("id", ArgumentIndex.First, classModel);
                                    if (currentId <= 0)
                                    {
                                        var uniqueIds = !isServerRpc ? uniqueClientIds : uniqueServerIds;
                                        currentId = uniqueIds[methodName];
                                    }

                                    methodBody.AppendLine($"// Id -> {currentId}");
                                    methodBody.AppendLine($"{methodName}({string.Join(", ", Enumerable.Repeat("default", parameters.Count))});");
                                    if (!GenHelper.IsManualRpc(method))
                                    {
                                        memberList.Add(GenerateRpcMethod(method, attribute, currentId));
                                        bool requiresPeer = isDualBehaviour || isClientBehaviour || isServerBehaviour;
                                        var directRpc = GenerateDirectRpcMethod(method, currentId, isServerRpc, requiresPeer: requiresPeer);
                                        var directPeerRpc = GenerateDirectPeerRpcMethod(method, currentId);
                                        if (classname != null)
                                        {
                                            relatedMemberList.Add(directRpc);
                                            if (isNetworkBehaviour && isClientRpc)
                                                relatedMemberList.Add(directPeerRpc);
                                        }
                                        else
                                        {
                                            memberList.Add(directRpc);
                                            if (isNetworkBehaviour && isClientRpc)
                                                memberList.Add(directPeerRpc);
                                        }
                                    }
                                }

                                methodBody.AppendLine("}");

                                // Create the dummy method
                                memberList.Add(
                                    MethodDeclaration(
                                        PredefinedType(Token(SyntaxKind.VoidKeyword)),
                                        "___PreventRpcStripping"
                                    )
                                    .WithModifiers(
                                        TokenList(
                                            new[]{
                                                Token(SyntaxKind.PrivateKeyword),
                                            }
                                        )
                                    )
                                    .WithAttributeLists(
                                        SingletonList(
                                            AttributeList(
                                               SeparatedList(
                                                    new AttributeSyntax[] {
                                                       Attribute(ParseName("Preserve")),
                                                       Attribute(ParseName("EditorBrowsable"), ParseAttributeArgumentList("(EditorBrowsableState.Never)")),
                                                       Attribute(IdentifierName("Obsolete"))
                                                        .WithArgumentList(
                                                            AttributeArgumentList(
                                                                SeparatedList<AttributeArgumentSyntax>(
                                                                    new SyntaxNodeOrToken[]
                                                                    {
                                                                        AttributeArgument(
                                                                            LiteralExpression(
                                                                                SyntaxKind.StringLiteralExpression,
                                                                                Literal("This method is reserved for exclusive use by the omni source generator."))),
                                                                        Token(SyntaxKind.CommaToken),
                                                                        AttributeArgument(
                                                                            LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                                                    }
                                                                )
                                                            )
                                                        )
                                                  }
                                               )
                                            )
                                        )
                                    )
                                    .WithBody(Block(ParseStatement(methodBody.ToString())))
                                );

                                parentClass = parentClass.AddMembers(memberList.ToArray());

                                ClassDeclarationSyntax relatedClass = null;
                                if (classname != null)
                                {
                                    relatedClass = ClassDeclaration(classname).WithModifiers(fromClass.Modifiers);
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

                                context.AddSource($"{parentClass.Identifier.Text}_rpc_usage_generated_code_.cs", Source.Clean(sb.ToString()));
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

        private MethodDeclarationSyntax GenerateDirectPeerRpcMethod(MethodDeclarationSyntax method, byte rpcId)
        {
            var originalParams = method.ParameterList.Parameters
                .Where(p =>
                    p.Type.ToString() != "NetworkPeer" &&
                    p.Type.ToString() != "Channel"
                )
                .ToList();

            var extraParams = new List<ParameterSyntax>
            {
                Parameter(Identifier("peer"))
                    .WithType(ParseTypeName("NetworkPeer")),
                Parameter(Identifier("group"))
                    .WithType(ParseTypeName("NetworkGroup"))
                    .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                Parameter(Identifier("target"))
                    .WithType(ParseTypeName("Target"))
                    .WithDefault(EqualsValueClause(ParseExpression("Target.Auto"))),
                Parameter(Identifier("mode"))
                    .WithType(ParseTypeName("DeliveryMode"))
                    .WithDefault(EqualsValueClause(ParseExpression("DeliveryMode.ReliableOrdered"))),
                Parameter(Identifier("channel"))
                    .WithType(ParseTypeName("int"))
                    .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))
            };

            var allParams = originalParams.Concat(extraParams);
            var methodName = $"{method.Identifier.Text}Rpc";

            // Create argument list for the RPC call
            var arguments = new List<ArgumentSyntax>
            {
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(rpcId))),
                Argument(IdentifierName("peer")),
                Argument(originalParams.Count > 0 ? IdentifierName("_buffer_") : IdentifierName("DataBuffer.Empty")),
                Argument(IdentifierName("group"))
            };

            var rpcCall = ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Server"),
                        IdentifierName("RpcViaPeer")
                    ),
                    ArgumentList(SeparatedList(arguments))
                )
            );

            var CallRpcParameters = ParseStatement($"Server.SetRpcParameters({rpcId}, mode, target, channel);");
            List<StatementSyntax> body = new List<StatementSyntax>
            {
                CallRpcParameters,
            };

            if (originalParams.Count > 0)
                body.Add(ParseStatement("using var _buffer_ = NetworkManager.Pool.Rent(enableTracking: false);"));

            foreach (var parameter in originalParams)
                body.Add(ParseStatement($"_buffer_.WriteAsBinary<{parameter.Type}>({parameter.Identifier.Text});"));

            body.Add(rpcCall);
            return MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    methodName
                )
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.PublicKeyword) }))
                .WithParameterList(
                    ParameterList(SeparatedList(allParams))
                )
                .WithBody(Block(body));
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
                    Parameter(Identifier("peer"))
                        .WithType(ParseTypeName("NetworkPeer"))
                );
            }

            if (!isServerRpc)
            {
                extraParams.Add(
                    Parameter(Identifier("group"))
                        .WithType(ParseTypeName("NetworkGroup"))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
                );
                extraParams.Add(
                    Parameter(Identifier("target"))
                        .WithType(ParseTypeName("Target"))
                        .WithDefault(EqualsValueClause(ParseExpression("Target.Auto")))
                );
            }

            extraParams.Add(
                Parameter(Identifier("mode"))
                    .WithType(ParseTypeName("DeliveryMode"))
                    .WithDefault(EqualsValueClause(ParseExpression("DeliveryMode.ReliableOrdered")))
            );
            extraParams.Add(
                Parameter(Identifier("channel"))
                    .WithType(ParseTypeName("int"))
                    .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))
            );

            if (isServerRpc)
                extraParams = extraParams.Where(p => p.Identifier.Text != "group" && p.Identifier.Text != "target").ToList();

            var allParams = originalParams.Concat(extraParams);

            var methodName = $"{method.Identifier.Text}Rpc";

            // Create argument list for the RPC call
            var arguments = new List<ArgumentSyntax>
            {
                Argument(LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(rpcId)))
            };

            if (requiresPeer && !isServerRpc)
                arguments.Add(Argument(IdentifierName("peer")));

            arguments.AddRange(originalParams.Select(p =>
                Argument(IdentifierName(p.Identifier.Text))));

            if (!isServerRpc)
            {
                arguments.Add(Argument(IdentifierName("group: group")));
            }

            var rpcCall = ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(isServerRpc ? "Client" : "Server"),
                        IdentifierName("Rpc")
                    ),
                    ArgumentList(SeparatedList(arguments))
                )
            );

            var CallRpcParameters = !isServerRpc ? ParseStatement($"Server.SetRpcParameters({rpcId}, mode, target, channel);") : ParseStatement($"Client.SetRpcParameters({rpcId}, mode, channel);");
            return MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    methodName
                )
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.PublicKeyword) }))
                .WithParameterList(
                    ParameterList(SeparatedList(allParams))
                )
                .WithBody(Block(new[] { CallRpcParameters, rpcCall }));
        }

        // read
        private MethodDeclarationSyntax GenerateRpcMethod(MethodDeclarationSyntax method, AttributeSyntax attribute, byte id)
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

                statements.Add(ParseStatement(statement));
                methodArguments.Add(Argument(IdentifierName(paramName)));
            }

            statements.Add(ExpressionStatement(
                InvocationExpression(
                    IdentifierName(method.Identifier.ToString()),
                    ArgumentList(SeparatedList(methodArguments))
                )
            ));

            return MethodDeclaration(method.ReturnType, Identifier(GenHelper.GenerateMethodName()))
            .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword)))
            .WithAttributeLists(
            SingletonList(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(attribute.Name)
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList(
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(id))))))))))
            .AddAttributeLists(
                AttributeList(
                   SeparatedList(
                        new AttributeSyntax[] {
                           Attribute(ParseName("Preserve")),
                           Attribute(ParseName("EditorBrowsable"), ParseAttributeArgumentList("(EditorBrowsableState.Never)")),
                           Attribute(IdentifierName("Obsolete"))
                           .WithArgumentList(
                               AttributeArgumentList(
                                   SeparatedList<AttributeArgumentSyntax>(
                                       new SyntaxNodeOrToken[]
                                       {
                                           AttributeArgument(
                                               LiteralExpression(
                                                   SyntaxKind.StringLiteralExpression,
                                                   Literal("This method is reserved for exclusive use by the omni source generator."))),
                                           Token(SyntaxKind.CommaToken),
                                           AttributeArgument(
                                               LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                       }
                                   )
                               )
                           )
                      }
                   )
                )
            )
            .WithParameterList(ParameterList(
                SeparatedList(
                    attribute.Name.ToString() == "Server"
                        ? new[] {
                                Parameter(Identifier("message"))
                                    .WithType(ParseTypeName("DataBuffer")),
                                Parameter(Identifier("peer"))
                                    .WithType(ParseTypeName("NetworkPeer")),
                                Parameter(Identifier("seqChannel"))
                                    .WithType(ParseTypeName("int"))
                        }
                        : new[] {
                                Parameter(Identifier("message"))
                                    .WithType(ParseTypeName("DataBuffer")),
                                Parameter(Identifier("seqChannel"))
                                    .WithType(ParseTypeName("int"))
                        }
                )
            ))
            .WithBody(Block(statements));
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