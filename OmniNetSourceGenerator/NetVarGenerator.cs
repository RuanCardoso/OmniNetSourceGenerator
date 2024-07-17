using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

                            SemanticModel semanticModel = context.Compilation.GetSemanticModel(
                                currentClassSyntax.SyntaxTree
                            );

                            if (currentClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                            {
                                if (
                                    currentClassSyntax.BaseList != null
                                    && currentClassSyntax.BaseList.Types.Any(x =>
                                        x.ToString() == "NetworkBehaviour"
                                        || x.ToString() == "DualBehaviour"
                                        || x.ToString() == "ClientBehaviour"
                                        || x.ToString() == "ServerBehaviour"
                                    )
                                )
                                {
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

                                    foreach (PropertyDeclarationSyntax prop in classWProp)
                                    {
                                        byte id = 0;
                                        TypeSyntax declarationType = prop.Type;
                                        TypeInfo typeInfo = semanticModel.GetTypeInfo(
                                            declarationType
                                        );

                                        var attributeSyntaxes = prop.AttributeLists.SelectMany(x =>
                                            x.Attributes.Where(y =>
                                                y.ArgumentList != null
                                                && y.ArgumentList.Arguments.Count > 0
                                                && y.Name.ToString() == "NetVar"
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

                                        sections.Add(
                                            CreateSection(
                                                id.ToString(),
                                                prop.Identifier.Text,
                                                declarationType.ToString()
                                            )
                                        );
                                    }

                                    MethodDeclarationSyntax onServerPropertyChanged =
                                        CreatePropertyMethod(
                                                "___OnServerPropertyChanged___",
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
                                                        "___OnPropertyChanged___(buffer, peer);"
                                                    )
                                                )
                                            );

                                    MethodDeclarationSyntax onClientPropertyChanged =
                                        CreatePropertyMethod(
                                                "___OnClientPropertyChanged___",
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
                                                        "___OnPropertyChanged___(buffer, default);"
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
                                                    "byte propertyId = buffer.Read<byte>();"
                                                ),
                                                SyntaxFactory
                                                    .SwitchStatement(
                                                        SyntaxFactory.ParseExpression("propertyId")
                                                    )
                                                    .WithSections(SyntaxFactory.List(sections)),
                                                SyntaxFactory.ParseStatement(
                                                    "base.___OnPropertyChanged___(buffer, peer);"
                                                )
                                            )
                                        );

                                    newClassSyntax = newClassSyntax.AddMembers(
                                        onServerPropertyChanged,
                                        onClientPropertyChanged,
                                        onPropertyChanged
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
                                        $"{currentClassSyntax.Identifier.ToFullString()}_netvar_gen_code.cs",
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

        private static SwitchSectionSyntax CreateSection(
            string caseExpression,
            string propertyName,
            string propertyType
        )
        {
            return SyntaxFactory.SwitchSection(
                SyntaxFactory.List(
                    new SwitchLabelSyntax[]
                    {
                        SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(caseExpression))
                    }
                ),
                SyntaxFactory.List(
                    new StatementSyntax[]
                    {
                        SyntaxFactory.Block(
                            SyntaxFactory.ParseStatement(
                                $"m_{propertyName} = buffer.FromBinary<{propertyType}>();"
                            ),
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
        internal List<PropertyDeclarationSyntax> propList = new List<PropertyDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is PropertyDeclarationSyntax propSyntax)
            {
                if (
                    propSyntax.AttributeLists.Any(x =>
                        x.Attributes.Any(y => y.Name.ToString() == "NetVar")
                    )
                )
                {
                    propList.Add(propSyntax);
                }
            }
        }
    }
}
