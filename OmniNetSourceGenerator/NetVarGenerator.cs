using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Generate serializer and deserialize methods.
// override ___OnPropertyChanged___ and etc.

namespace OmniNetSourceGenerator
{
    [Generator]
    internal class NetVarGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.SyntaxReceiver is NetVarSyntaxReceiver syntaxReceiver)
                {
                    if (syntaxReceiver.propList.Any())
                    {
                        var classWithProps = syntaxReceiver.propList.GroupBy(x =>
                            (ClassDeclarationSyntax)x.Parent
                        );

                        foreach (var classWProp in classWithProps)
                        {
                            ClassDeclarationSyntax currentClassSyntax = classWProp.Key;

                            #region Usings
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("#nullable disable");
                            sb.AppendLine("#pragma warning disable");
                            sb.AppendLine("using Newtonsoft.Json;");
                            sb.AppendLine("using MemoryPack;");

                            IEnumerable<UsingDirectiveSyntax> usingSyntaxes = currentClassSyntax
                                .SyntaxTree.GetRoot()
                                .DescendantNodes()
                                .OfType<UsingDirectiveSyntax>();

                            foreach (UsingDirectiveSyntax usingSyntax in usingSyntaxes)
                            {
                                sb.AppendLine(usingSyntax.ToString());
                            }
                            #endregion

                            if (currentClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                            {
                                if (
                                    currentClassSyntax.BaseList != null
                                    && currentClassSyntax.BaseList.Types.Any(x =>
                                        x.ToString() == "NetworkBehaviour"
                                        || x.ToString().Contains("Base") // Base is for networkbehaviour(identified by Base)
                                        || x.ToString() == "DualBehaviour"
                                        || x.ToString() == "ClientBehaviour"
                                        || x.ToString() == "ServerBehaviour"
                                    )
                                )
                                {
                                    string baseClassName = currentClassSyntax
                                        .BaseList.Types[0]
                                        .ToString();

                                    string isServer = "false";
                                    if (baseClassName == "NetworkBehaviour")
                                    {
                                        isServer = "IsServer";
                                    }
                                    else if (baseClassName == "ServerBehaviour")
                                    {
                                        isServer = "true";
                                    }
                                    else if (baseClassName == "ClientBehaviour")
                                    {
                                        isServer = "false";
                                    }
                                    else
                                    {
                                        isServer = "IsServer";
                                    }

                                    NamespaceDeclarationSyntax currentNamespaceSyntax =
                                        currentClassSyntax.Parent
                                            is NamespaceDeclarationSyntax @namespace
                                            ? @namespace
                                            : null;

                                    NamespaceDeclarationSyntax newNamespaceSyntax =
                                        SyntaxFactory.NamespaceDeclaration(
                                            currentNamespaceSyntax != null
                                                ? currentNamespaceSyntax.Name
                                                : SyntaxFactory.ParseName("@UNDEFINED")
                                        );

                                    ClassDeclarationSyntax newClassSyntax = SyntaxFactory
                                        .ClassDeclaration(currentClassSyntax.Identifier.Text)
                                        .WithModifiers(currentClassSyntax.Modifiers)
                                        .WithBaseList(currentClassSyntax.BaseList);

                                    List<SwitchSectionSyntax> sections =
                                        new List<SwitchSectionSyntax>();

                                    List<MethodDeclarationSyntax> onChangedHandlers =
                                        new List<MethodDeclarationSyntax>();

                                    List<StatementSyntax> onNotifyHandlers =
                                        new List<StatementSyntax>();

                                    HashSet<byte> ids = new HashSet<byte>();

                                    foreach (MemberDeclarationSyntax member in classWProp)
                                    {
                                        byte id = 0;
                                        TypeSyntax declarationType = member
                                            is FieldDeclarationSyntax field
                                            ? field.Declaration.Type
                                            : ((PropertyDeclarationSyntax)member).Type;

                                        SemanticModel model = context.Compilation.GetSemanticModel(
                                            member.SyntaxTree
                                        );

                                        bool isSerializable = IsSerializable(
                                            model.GetTypeInfo(declarationType).Type,
                                            out bool withPeer
                                        );

                                        var attributeSyntaxes = member.AttributeLists.SelectMany(
                                            x =>
                                                x.Attributes.Where(y =>
                                                    y.ArgumentList != null
                                                    && y.ArgumentList.Arguments.Count > 0
                                                    && y.Name.ToString() == "NetworkVariable"
                                                )
                                        );

                                        if (attributeSyntaxes.Any())
                                        {
                                            foreach (var attributeSyntax in attributeSyntaxes)
                                            {
                                                var arguments = attributeSyntax
                                                    .ArgumentList
                                                    .Arguments;

                                                var idTypeExpression =
                                                    Helper.GetArgumentExpression<LiteralExpressionSyntax>(
                                                        "id",
                                                        0,
                                                        arguments
                                                    );

                                                if (idTypeExpression != null)
                                                {
                                                    if (
                                                        byte.TryParse(
                                                            idTypeExpression.Token.ValueText,
                                                            out byte idValue
                                                        )
                                                    )
                                                    {
                                                        id = idValue;
                                                    }
                                                }
                                            }
                                        }

                                        if (baseClassName.Contains("Base"))
                                        {
                                            if (id <= 0)
                                            {
                                                id = 101;
                                            }
                                        }

                                        if (id <= 0)
                                        {
                                            id++;
                                            while (ids.Contains(id))
                                            {
                                                id++;
                                            }
                                        }

                                        if (member is PropertyDeclarationSyntax propSyntax)
                                        {
                                            while (ids.Contains(id))
                                            {
                                                id++;
                                            }

                                            ids.Add(id);
                                            sections.Add(
                                                CreateSection(
                                                    id.ToString(),
                                                    propSyntax.Identifier.Text,
                                                    declarationType.ToString(),
                                                    isSerializable,
                                                    withPeer,
                                                    isServer
                                                )
                                            );

                                            onChangedHandlers.Add(
                                                CreateHandler(
                                                    propSyntax.Identifier.Text,
                                                    declarationType.ToString()
                                                )
                                            );

                                            onChangedHandlers.Add(
                                                CreateSyncMethod(
                                                    propSyntax.Identifier.Text,
                                                    id.ToString(),
                                                    baseClassName
                                                )
                                            );

                                            onNotifyHandlers.Add(
                                                SyntaxFactory.ParseStatement(
                                                    $"{propSyntax.Identifier.Text} = m_{propSyntax.Identifier.Text};"
                                                )
                                            );

                                            onNotifyHandlers.Add(
                                                SyntaxFactory.ParseStatement(
                                                    $"base.___NotifyChange___();"
                                                )
                                            );
                                        }
                                        else if (member is FieldDeclarationSyntax fieldSyntax)
                                        {
                                            foreach (
                                                var variable in fieldSyntax.Declaration.Variables
                                            )
                                            {
                                                // remove m_ prefix
                                                string variableName =
                                                    variable.Identifier.Text.Substring(2);

                                                while (ids.Contains(id))
                                                {
                                                    id++;
                                                }

                                                ids.Add(id);
                                                sections.Add(
                                                    CreateSection(
                                                        id.ToString(),
                                                        variableName,
                                                        declarationType.ToString(),
                                                        isSerializable,
                                                        withPeer,
                                                        isServer
                                                    )
                                                );

                                                onChangedHandlers.Add(
                                                    CreateHandler(
                                                        variableName,
                                                        declarationType.ToString()
                                                    )
                                                );

                                                onChangedHandlers.Add(
                                                    CreateSyncMethod(
                                                        variableName,
                                                        id.ToString(),
                                                        baseClassName
                                                    )
                                                );

                                                onNotifyHandlers.Add(
                                                    SyntaxFactory.ParseStatement(
                                                        $"{variableName} = m_{variableName};"
                                                    )
                                                );

                                                onNotifyHandlers.Add(
                                                    SyntaxFactory.ParseStatement(
                                                        $"base.___NotifyChange___();"
                                                    )
                                                );
                                            }
                                        }
                                    }

                                    MethodDeclarationSyntax onServerPropertyChanged =
                                        CreatePropertyMethod(
                                                $"___OnServerPropertyChanged___{currentClassSyntax.Identifier.Text}___",
                                                "Server"
                                            )
                                            .WithParameterList(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(
                                                        new ParameterSyntax[]
                                                        {
                                                            SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier(
                                                                    "DataBuffer buffer"
                                                                )
                                                            ),
                                                            SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier(
                                                                    "NetworkPeer peer"
                                                                )
                                                            )
                                                        }
                                                    )
                                                )
                                            )
                                            .WithBody(
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.ParseStatement(
                                                        "___OnPropertyChanged___(default, default, peer, buffer);"
                                                    )
                                                )
                                            );

                                    MethodDeclarationSyntax onClientPropertyChanged =
                                        CreatePropertyMethod(
                                                $"___OnClientPropertyChanged___{currentClassSyntax.Identifier.Text}___",
                                                "Client"
                                            )
                                            .WithParameterList(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(
                                                        new ParameterSyntax[]
                                                        {
                                                            SyntaxFactory.Parameter(
                                                                SyntaxFactory.Identifier(
                                                                    "DataBuffer buffer"
                                                                )
                                                            ),
                                                        }
                                                    )
                                                )
                                            )
                                            .WithBody(
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.ParseStatement(
                                                        "___OnPropertyChanged___(default, default, default, buffer);"
                                                    )
                                                )
                                            );

                                    MethodDeclarationSyntax onPropertyChanged = SyntaxFactory
                                        .MethodDeclaration(
                                            SyntaxFactory.ParseTypeName("void"),
                                            "___OnPropertyChanged___"
                                        )
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
                                            )
                                        )
                                        .WithParameterList(
                                            SyntaxFactory.ParameterList(
                                                SyntaxFactory.SeparatedList(
                                                    new ParameterSyntax[]
                                                    {
                                                        SyntaxFactory.Parameter(
                                                            SyntaxFactory.Identifier(
                                                                "string propertyName"
                                                            )
                                                        ),
                                                        SyntaxFactory.Parameter(
                                                            SyntaxFactory.Identifier(
                                                                "byte propertyId"
                                                            )
                                                        ),
                                                        SyntaxFactory.Parameter(
                                                            SyntaxFactory.Identifier(
                                                                "NetworkPeer peer"
                                                            )
                                                        ),
                                                        SyntaxFactory.Parameter(
                                                            SyntaxFactory.Identifier(
                                                                "DataBuffer buffer"
                                                            )
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                        .WithBody(
                                            SyntaxFactory.Block(
                                                SyntaxFactory.ParseStatement(
                                                    "propertyId = buffer.Read<byte>();"
                                                ),
                                                SyntaxFactory
                                                    .SwitchStatement(
                                                        SyntaxFactory.ParseExpression("propertyId")
                                                    )
                                                    .WithSections(SyntaxFactory.List(sections)),
                                                SyntaxFactory.ParseStatement(
                                                    "buffer.SeekToBegin();"
                                                ),
                                                SyntaxFactory.ParseStatement(
                                                    "base.___OnPropertyChanged___(propertyName, propertyId, peer, buffer);"
                                                )
                                            )
                                        );

                                    MethodDeclarationSyntax onNotifyChange = SyntaxFactory
                                        .MethodDeclaration(
                                            SyntaxFactory.ParseTypeName("void"),
                                            "___NotifyChange___"
                                        )
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
                                            )
                                        )
                                        .WithBody(
                                            SyntaxFactory.Block(
                                                SyntaxFactory.List(onNotifyHandlers)
                                            )
                                        );

                                    newClassSyntax = newClassSyntax.AddMembers(
                                        onServerPropertyChanged,
                                        onClientPropertyChanged,
                                        onPropertyChanged,
                                        onNotifyChange
                                    );

                                    newClassSyntax = newClassSyntax.AddMembers(
                                        onChangedHandlers.ToArray()
                                    );

                                    newNamespaceSyntax = newNamespaceSyntax.AddMembers(
                                        newClassSyntax
                                    );

                                    if (currentNamespaceSyntax == null)
                                    {
                                        sb.Append(newClassSyntax.NormalizeWhitespace().ToString());
                                    }
                                    else
                                    {
                                        sb.Append(
                                            newNamespaceSyntax.NormalizeWhitespace().ToString()
                                        );
                                    }

                                    context.AddSource(
                                        $"{currentClassSyntax.Identifier.ToFullString()}_netvar_generated_code.cs",
                                        sb.ToString()
                                    );
                                }
                                else
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            new DiagnosticDescriptor(
                                                "CB004",
                                                "Inheritance Requirement",
                                                "The class must inherit from `EventBehaviour` or `NetworkBehaviour` to ensure proper network functionality.",
                                                "Design",
                                                DiagnosticSeverity.Error,
                                                isEnabledByDefault: true
                                            ),
                                            Location.None
                                        )
                                    );
                                }
                            }
                            else
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        new DiagnosticDescriptor(
                                            "CB003",
                                            "Code Quality Issue",
                                            "The class definition is missing the 'partial' keyword, which is required for this context.",
                                            "Design",
                                            DiagnosticSeverity.Error,
                                            isEnabledByDefault: true
                                        ),
                                        Location.None
                                    )
                                );
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
                            "CB001",
                            "Unhandled Exception",
                            $"An unhandled exception occurred: {ex.Message}\nStack Trace: {ex.StackTrace}",
                            "Runtime",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true
                        ),
                        Location.None
                    )
                );
            }
        }

        private bool IsSerializable(ITypeSymbol typeSymbol, out bool withPeer)
        {
            withPeer = false;
            if (typeSymbol != null)
            {
                var interfaces = typeSymbol.Interfaces;

                withPeer = interfaces.Any(x => x.Name == "IMessageWithPeer");
                return interfaces.Any(x =>
                    x.Name == "IMessageWithPeer" || x.Name == "IMessage"
                );
            }

            return false;
        }

        private MethodDeclarationSyntax CreateSyncMethod(
            string propertyName,
            string propertyId,
            string baseClassName
        )
        {
            return SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"Sync{propertyName}")
                .WithModifiers(
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                )
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            new ParameterSyntax[]
                            {
                                SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier($"options"))
                                    .WithType(SyntaxFactory.ParseTypeName("NetworkVariableOptions"))
                            }
                        )
                    )
                )
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement("options ??= new();"),
                        (baseClassName == "NetworkBehaviour" || baseClassName.Contains("Base"))
                            ? SyntaxFactory
                                .IfStatement(
                                    SyntaxFactory.ParseExpression("IsMine"),
                                    SyntaxFactory.Block(
                                        SyntaxFactory.ParseStatement(
                                            $"Local.ManualSync({propertyName}, {propertyId}, options);"
                                        )
                                    )
                                )
                                .WithElse(
                                    SyntaxFactory.ElseClause(
                                        SyntaxFactory.Block(
                                            SyntaxFactory.IfStatement(
                                                SyntaxFactory.ParseExpression("IsServer"),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.ParseStatement(
                                                        $"Remote.ManualSync({propertyName}, {propertyId}, options);"
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            : baseClassName == "ServerBehaviour"
                                ? SyntaxFactory.ParseStatement(
                                    $"Remote.ManualSync({propertyName}, {propertyId}, options);"
                                )
                                : baseClassName == "ClientBehaviour"
                                    ? SyntaxFactory.ParseStatement(
                                        $"Local.ManualSync({propertyName}, {propertyId}, options);"
                                    )
                                    : null
                    )
                );
        }

        private MethodDeclarationSyntax CreateHandler(string propertyName, string type)
        {
            return SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"On{propertyName}Changed")
                .WithModifiers(
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                )
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            new ParameterSyntax[]
                            {
                                SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier($"prev{propertyName}"))
                                    .WithType(SyntaxFactory.ParseTypeName(type)),
                                SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier($"next{propertyName}"))
                                    .WithType(SyntaxFactory.ParseTypeName(type)),
                                SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier("isWriting"))
                                    .WithType(SyntaxFactory.ParseTypeName("bool"))
                            }
                        )
                    )
                )
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private SwitchSectionSyntax CreateSection(
            string caseExpression,
            string propertyName,
            string propertyType,
            bool isSerializable,
            bool isSerializableWithPeer,
            string isServer
        )
        {
            return !isSerializable
                ? SyntaxFactory.SwitchSection(
                    SyntaxFactory.List(
                        new SwitchLabelSyntax[]
                        {
                            SyntaxFactory.CaseSwitchLabel(
                                SyntaxFactory.ParseExpression(caseExpression)
                            )
                        }
                    ),
                    SyntaxFactory.List(
                        new StatementSyntax[]
                        {
                            SyntaxFactory.Block(
                                SyntaxFactory.ParseStatement($"propertyName = \"{propertyName}\";"),
                                SyntaxFactory.ParseStatement(
                                    $"var nextValue = buffer.ReadAsBinary<{propertyType}>();"
                                ),
                                SyntaxFactory.ParseStatement(
                                    $"On{propertyName}Changed(m_{propertyName}, nextValue, false);"
                                ),
                                SyntaxFactory.ParseStatement($"m_{propertyName} = nextValue;"),
                                SyntaxFactory.BreakStatement()
                            ),
                        }
                    )
                ) // Start of deserialize
                : SyntaxFactory.SwitchSection(
                    SyntaxFactory.List(
                        new SwitchLabelSyntax[]
                        {
                            SyntaxFactory.CaseSwitchLabel(
                                SyntaxFactory.ParseExpression(caseExpression)
                            )
                        }
                    ),
                    SyntaxFactory.List(
                        new StatementSyntax[]
                        {
                            SyntaxFactory.Block(
                                SyntaxFactory.ParseStatement($"propertyName = \"{propertyName}\";"),
                                SyntaxFactory.ParseStatement(
                                    "using var nBuffer = NetworkManager.Pool.Rent();"
                                ),
                                SyntaxFactory.ParseStatement(
                                    "nBuffer.RawWrite(buffer.GetSpan().Slice(0, buffer.Length)); // from current position to end > skip propertyId(header)"
                                ),
                                SyntaxFactory.ParseStatement($"nBuffer.SeekToBegin();"),
                                isSerializableWithPeer
                                    ? SyntaxFactory.ParseStatement(
                                        $"var nextValue = nBuffer.Deserialize<{propertyType}>(peer != null ? peer : NetworkManager.LocalPeer, {isServer});"
                                    )
                                    : SyntaxFactory.ParseStatement(
                                        $"var nextValue = nBuffer.Deserialize<{propertyType}>();"
                                    ),
                                SyntaxFactory.ParseStatement(
                                    $"On{propertyName}Changed(m_{propertyName}, nextValue, false);"
                                ),
                                SyntaxFactory.ParseStatement($"m_{propertyName} = nextValue;"),
                                SyntaxFactory.BreakStatement()
                            ),
                        }
                    )
                );
        }

        private MethodDeclarationSyntax CreatePropertyMethod(
            string methodName,
            string attributeName
        )
        {
            MethodDeclarationSyntax onPropertyChanged = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), methodName)
                .WithAttributeLists(
                    SyntaxFactory.List(
                        new AttributeListSyntax[]
                        {
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory
                                        .Attribute(SyntaxFactory.IdentifierName(attributeName))
                                        .WithArgumentList(
                                            SyntaxFactory.AttributeArgumentList(
                                                SyntaxFactory.SeparatedList(
                                                    new AttributeArgumentSyntax[]
                                                    {
                                                        SyntaxFactory.AttributeArgument(
                                                            SyntaxFactory.ParseExpression("255")
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                )
                            )
                        }
                    )
                )
                .WithModifiers(
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                );

            return onPropertyChanged;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new NetVarSyntaxReceiver());
        }
    }

    internal class NetVarSyntaxReceiver : ISyntaxReceiver
    {
        internal List<MemberDeclarationSyntax> propList = new List<MemberDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MemberDeclarationSyntax propSyntax)
            {
                if (
                    propSyntax.AttributeLists.Any(x =>
                        x.Attributes.Any(y => y.Name.ToString() == "NetworkVariable")
                    )
                )
                {
                    propList.Add(propSyntax);
                }
            }
        }
    }
}
