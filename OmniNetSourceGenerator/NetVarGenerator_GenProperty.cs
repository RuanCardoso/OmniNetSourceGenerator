using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Generate property to a field with ManualSync...

namespace OmniNetSourceGenerator
{
    [Generator]
    internal class NetVarGeneratorGenProperty : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.SyntaxReceiver is NetVarGenPropertySyntaxReceiver syntaxReceiver)
                {
                    if (syntaxReceiver.fieldList.Any())
                    {
                        var classWithFields = syntaxReceiver.fieldList.GroupBy(x =>
                            (ClassDeclarationSyntax)x.Parent
                        );

                        foreach (var classWField in classWithFields)
                        {
                            ClassDeclarationSyntax currentClassSyntax = classWField.Key;

                            #region Usings
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("#nullable disable");

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
                                    string baseClassName = currentClassSyntax
                                        .BaseList.Types[0]
                                        .ToString();

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

                                    List<MemberDeclarationSyntax> memberList =
                                        new List<MemberDeclarationSyntax>();

                                    foreach (FieldDeclarationSyntax field in classWField)
                                    {
                                        byte id = 0;
                                        TypeSyntax declarationType = field.Declaration.Type;
                                        TypeInfo typeInfo = semanticModel.GetTypeInfo(
                                            declarationType
                                        );

                                        var attributeSyntaxes = field.AttributeLists.SelectMany(x =>
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

                                        if (field.Declaration.Variables.Count > 1)
                                        {
                                            id = 150;
                                        }

                                        if (baseClassName == "DualBehaviour")
                                        {
                                            context.ReportDiagnostic(
                                                Diagnostic.Create(
                                                    new DiagnosticDescriptor(
                                                        "CB008",
                                                        "Not Supported!",
                                                        "Only 'NetworkBehaviour', 'ClientBehaviour' and 'ServerBehaviour' are supported with auto generated properties. DualBehaviour is supported, but only with manually created properties. Please use 'DualBehaviour' with manually created properties.",
                                                        "Design",
                                                        DiagnosticSeverity.Error,
                                                        isEnabledByDefault: true
                                                    ),
                                                    Location.None
                                                )
                                            );

                                            return;
                                        }

                                        foreach (
                                            VariableDeclaratorSyntax variableSyntax in field
                                                .Declaration
                                                .Variables
                                        )
                                        {
                                            string variableName = variableSyntax.Identifier.Text;

                                            if (!variableName.StartsWith("m_"))
                                            {
                                                context.ReportDiagnostic(
                                                    Diagnostic.Create(
                                                        new DiagnosticDescriptor(
                                                            "CB002",
                                                            "Invalid Property Name",
                                                            "Field name must start with \"m_\". like as \"m_Health\". Ensure that the name is correct.",
                                                            "Design",
                                                            DiagnosticSeverity.Error,
                                                            isEnabledByDefault: true
                                                        ),
                                                        Location.None
                                                    )
                                                );

                                                return;
                                            }

                                            // remove m_ prefix
                                            variableName = variableName.Substring(2);

                                            if (!char.IsUpper(variableName[0]))
                                            {
                                                context.ReportDiagnostic(
                                                    Diagnostic.Create(
                                                        new DiagnosticDescriptor(
                                                            "CB003",
                                                            "Invalid Property Name",
                                                            "The first letter after \"m_\" must be capitalized like as \"m_Power\". Ensure that the name is correct.",
                                                            "Design",
                                                            DiagnosticSeverity.Error,
                                                            isEnabledByDefault: true
                                                        ),
                                                        Location.None
                                                    )
                                                );

                                                return;
                                            }

                                            memberList.Add(
                                                SyntaxFactory
                                                    .FieldDeclaration(
                                                        SyntaxFactory.VariableDeclaration(
                                                            SyntaxFactory.ParseTypeName(
                                                                $"SyncOptions"
                                                            ),
                                                            SyntaxFactory.SeparatedList(
                                                                new VariableDeclaratorSyntax[]
                                                                {
                                                                    SyntaxFactory
                                                                        .VariableDeclarator(
                                                                            $"{variableName}Options"
                                                                        )
                                                                        .WithInitializer(
                                                                            SyntaxFactory.EqualsValueClause(
                                                                                SyntaxFactory.ParseExpression(
                                                                                    $"SyncOptions.Default"
                                                                                )
                                                                            )
                                                                        )
                                                                }
                                                            )
                                                        )
                                                    )
                                                    .WithModifiers(
                                                        SyntaxFactory.TokenList(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.PrivateKeyword
                                                            )
                                                        )
                                                    )
                                            );

                                            memberList.Add(
                                                SyntaxFactory
                                                    .PropertyDeclaration(
                                                        declarationType,
                                                        variableName
                                                    )
                                                    .WithAttributeLists(
                                                        SyntaxFactory.List(
                                                            new AttributeListSyntax[]
                                                            {
                                                                SyntaxFactory.AttributeList(
                                                                    SyntaxFactory.SeparatedList(
                                                                        new AttributeSyntax[]
                                                                        {
                                                                            SyntaxFactory.Attribute(
                                                                                SyntaxFactory.ParseName(
                                                                                    $"NetVar"
                                                                                ),
                                                                                SyntaxFactory.AttributeArgumentList(
                                                                                    SyntaxFactory.SeparatedList(
                                                                                        new AttributeArgumentSyntax[]
                                                                                        {
                                                                                            SyntaxFactory.AttributeArgument(
                                                                                                SyntaxFactory.ParseExpression(
                                                                                                    $"{id}"
                                                                                                )
                                                                                            )
                                                                                        }
                                                                                    )
                                                                                )
                                                                            )
                                                                        }
                                                                    )
                                                                )
                                                            }
                                                        )
                                                    )
                                                    .WithModifiers(
                                                        SyntaxFactory.TokenList(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.PublicKeyword
                                                            )
                                                        )
                                                    )
                                                    .WithAccessorList(
                                                        SyntaxFactory.AccessorList(
                                                            SyntaxFactory.List(
                                                                new AccessorDeclarationSyntax[]
                                                                {
                                                                    SyntaxFactory
                                                                        .AccessorDeclaration(
                                                                            SyntaxKind.GetAccessorDeclaration
                                                                        )
                                                                        .WithBody(
                                                                            SyntaxFactory.Block(
                                                                                SyntaxFactory.ParseStatement(
                                                                                    $"return m_{variableName};"
                                                                                )
                                                                            )
                                                                        ),
                                                                    SyntaxFactory
                                                                        .AccessorDeclaration(
                                                                            SyntaxKind.SetAccessorDeclaration
                                                                        )
                                                                        .WithBody(
                                                                            SyntaxFactory.Block(
                                                                                SyntaxFactory.ParseStatement(
                                                                                    $"m_{variableName} = value;"
                                                                                ),
                                                                                baseClassName
                                                                                == "NetworkBehaviour"
                                                                                    ? SyntaxFactory
                                                                                        .IfStatement(
                                                                                            SyntaxFactory.ParseExpression(
                                                                                                "IsMine"
                                                                                            ),
                                                                                            SyntaxFactory.Block(
                                                                                                SyntaxFactory.ParseStatement(
                                                                                                    $"Local.ManualSync({variableName}, {id}, {variableName}Options);"
                                                                                                )
                                                                                            )
                                                                                        )
                                                                                        .WithElse(
                                                                                            SyntaxFactory.ElseClause(
                                                                                                SyntaxFactory.Block(
                                                                                                    SyntaxFactory.ParseStatement(
                                                                                                        $"Remote.ManualSync({variableName}, {id}, {variableName}Options);"
                                                                                                    )
                                                                                                )
                                                                                            )
                                                                                        )
                                                                                    : baseClassName
                                                                                    == "ServerBehaviour"
                                                                                        ? SyntaxFactory.ParseStatement(
                                                                                            $"Remote.ManualSync({variableName}, {id}, {variableName}Options);"
                                                                                        )
                                                                                        : baseClassName
                                                                                        == "ClientBehaviour"
                                                                                            ? SyntaxFactory.ParseStatement(
                                                                                                $"Local.ManualSync({variableName}, {id}, {variableName}Options);"
                                                                                            )
                                                                                            : null
                                                                            )
                                                                        )
                                                                }
                                                            )
                                                        )
                                                    )
                                            );

                                            id++;
                                        }
                                    }

                                    newClassSyntax = newClassSyntax.AddMembers(
                                        memberList.ToArray()
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
                                        $"{currentClassSyntax.Identifier.ToFullString()}_netvar_create_property_generated_code.cs",
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

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new NetVarGenPropertySyntaxReceiver());
        }
    }

    internal class NetVarGenPropertySyntaxReceiver : ISyntaxReceiver
    {
        internal List<FieldDeclarationSyntax> fieldList = new List<FieldDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is FieldDeclarationSyntax fieldSyntax)
            {
                if (
                    fieldSyntax.AttributeLists.Any(x =>
                        x.Attributes.Any(y => y.Name.ToString() == "NetVar")
                    )
                )
                {
                    fieldList.Add(fieldSyntax);
                }
            }
        }
    }
}
