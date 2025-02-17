using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;

namespace OmniNetSourceGenerator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class NetworkVariableCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            GenHelper.InvalidFieldNamingConventionIsUpper.Id,
            GenHelper.InvalidFieldNamingConventionStartsWith.Id,
            GenHelper.PartialKeywordMissing.Id,
            NetworkVariableAnalyzer.NetworkVariableFieldShouldBePrivate.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == GenHelper.InvalidFieldNamingConventionIsUpper.Id)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Omni -> Make field name start with 'm_' and use PascalCase",
                        createChangedDocument: token => FixInvalidFieldNamingConventionIsUpper(context.Document, declaration, token),
                        equivalenceKey: GenHelper.InvalidFieldNamingConventionIsUpper.Title.ToString()),
                    diagnostic);
            }
            else if (diagnostic.Id == GenHelper.PartialKeywordMissing.Id)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Omni -> Add 'partial' keyword to class",
                        createChangedDocument: token => FixPartialKeywordMissing(context.Document, declaration, token),
                        equivalenceKey: GenHelper.PartialKeywordMissing.Title.ToString()),
                    diagnostic);
            }
            else if (diagnostic.Id == GenHelper.InvalidFieldNamingConventionStartsWith.Id)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Omni -> Make field name start with 'm_' and use PascalCase",
                        createChangedDocument: token => FixInvalidFieldNamingConventionIsUpper(context.Document, declaration, token),
                        equivalenceKey: GenHelper.InvalidFieldNamingConventionStartsWith.Title.ToString()),
                    diagnostic);
            }
            else if (diagnostic.Id == NetworkVariableAnalyzer.NetworkVariableFieldShouldBePrivate.Id)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Omni -> Add 'private' modifier to network variable field",
                        createChangedDocument: token => FixNetworkVariableFieldShouldBePrivate(context.Document, declaration, token),
                        equivalenceKey: NetworkVariableAnalyzer.NetworkVariableFieldShouldBePrivate.Title.ToString()),
                    diagnostic);
            }
        }

        private async Task<Document> FixInvalidFieldNamingConventionIsUpper(Document document, VariableDeclaratorSyntax declaration, CancellationToken cancellationToken)
        {
            string fieldName = declaration.Identifier.Text;
            if (fieldName.StartsWith("m_"))
            {
                fieldName = "m_" + char.ToUpper(fieldName[2]) + fieldName.Substring(3);
            }
            else
            {
                fieldName = "m_" + char.ToUpper(fieldName[0]) + fieldName.Substring(1);
            }

            var newDeclaration = declaration.WithIdentifier(SyntaxFactory.Identifier(fieldName));
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> FixPartialKeywordMissing(Document document, ClassDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var newDeclaration = declaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> FixNetworkVariableFieldShouldBePrivate(Document document, FieldDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var newDeclaration = declaration.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkVariableAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor NetworkVariableFieldShouldBePrivate = new DiagnosticDescriptor(
            id: "OMNI018",
            title: "Network Variable Field Should Be Private",
            messageFormat: "Network variable field '{0}' should be private as it is exposed through a generated public property",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Network variable fields should be declared as private since they are automatically exposed " +
                        "through generated public properties. This follows encapsulation best practices and avoids " +
                        "having two public access points to the same data and best Unity Inspector integration."
        );

        public static readonly DiagnosticDescriptor NonValueTypeEqualityCheck = new DiagnosticDescriptor(
            id: "OMNI020",
            title: "Performance: Reference Type Equality Check",
            messageFormat: "Network variable '{0}' of type '{1}' has CheckEquality enabled which may impact performance for reference types",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[] {
            GenHelper.InvalidFieldNamingConventionIsUpper,
            GenHelper.InvalidFieldNamingConventionStartsWith,
            GenHelper.InheritanceConstraintViolation,
            GenHelper.PartialKeywordMissing,
            NetworkVariableFieldShouldBePrivate,
            NonValueTypeEqualityCheck
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptors);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is FieldDeclarationSyntax field)
            {
                if (field.HasAttribute("NetworkVariable"))
                {
                    SemanticModel semanticModel = context.SemanticModel;
                    var typeInfo = semanticModel.GetTypeInfo(field.Declaration.Type);
                    Context cContext = new Context(context);

                    if (field.Parent is ClassDeclarationSyntax @class)
                    {
                        var singleField = field.Declaration.Variables.First();
                        if (!field.HasModifier(SyntaxKind.PrivateKeyword))
                        {
                            cContext.ReportDiagnostic(
                                NetworkVariableFieldShouldBePrivate,
                                field.GetLocation(),
                                singleField.Identifier.Text
                            );
                        }

                        if (typeInfo.Type != null && !typeInfo.Type.IsValueType)
                        {
                            AttributeSyntax attribute = field.GetAttribute("NetworkVariable");
                            if (attribute != null)
                            {
                                var equalExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("CheckEquality", ArgumentIndex.None);
                                if (equalExpression != null)
                                {
                                    if (bool.TryParse(equalExpression.Token.ValueText, out bool isEquality))
                                    {
                                        if (isEquality)
                                        {
                                            context.ReportDiagnostic(
                                                Diagnostic.Create(
                                                    NonValueTypeEqualityCheck,
                                                    singleField.GetLocation(),
                                                    singleField.Identifier.Text,
                                                    typeInfo.Type.Name
                                                )
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonValueTypeEqualityCheck,
                                            singleField.GetLocation(),
                                            singleField.Identifier.Text,
                                            typeInfo.Type.Name
                                        )
                                    );
                                }
                            }
                        }

                        bool isNetworkBehaviour = @class.InheritsFromClass(semanticModel, "NetworkBehaviour");
                        bool isClientBehaviour = @class.InheritsFromClass(semanticModel, "ClientBehaviour");
                        bool isServerBehaviour = @class.InheritsFromClass(semanticModel, "ServerBehaviour");

                        GenHelper.ReportPartialKeywordRequirement(new Context(context), @class, @class.GetLocation());
                        foreach (var variable in field.Declaration.Variables)
                        {
                            string fieldName = variable.Identifier.Text;
                            string fieldNameWithoutPrefix = fieldName.Substring(2);

                            Location location = variable.GetLocation();
                            if (!GenHelper.ReportInvalidFieldNamingStartsWith(cContext, fieldName, location))
                            {
                                if (!GenHelper.ReportInvalidFieldNamingIsUpper(cContext, fieldNameWithoutPrefix, location))
                                {
                                    // Note: DualBehaviour is not supported. It is a manual class that should not be used with auto-generated properties.
                                    if (!isNetworkBehaviour && !isClientBehaviour && !isServerBehaviour)
                                    {
                                        GenHelper.ReportInheritanceRequirement(cContext, @class.Identifier.Text, location);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkVariableDuplicateIdAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DuplicateNetworkVariableId = new DiagnosticDescriptor(
            id: "OMNI019",
            title: "Duplicate Network Variable ID",
            messageFormat: "Network variable with ID {0} is already defined in {1}",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Network variables must have unique IDs within the inheritance hierarchy to ensure proper network synchronization."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DuplicateNetworkVariableId);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ClassDeclarationSyntax @class)
            {
                var semanticModel = context.SemanticModel;
                var classSymbol = semanticModel.GetDeclaredSymbol(@class);
                if (classSymbol == null)
                    return;

                var networkVariables = new Dictionary<byte, (string FieldName, Location Location, string ClassName)>();
                CollectNetworkVariables(new Context(context), classSymbol, networkVariables, semanticModel);
            }
        }

        private void CollectNetworkVariables(
            Context context,
            INamedTypeSymbol classSymbol,
            Dictionary<byte, (string FieldName, Location Location, string ClassName)> networkVariables,
            SemanticModel semanticModel)
        {
            if (classSymbol.BaseType != null)
                CollectNetworkVariables(context, classSymbol.BaseType, networkVariables, semanticModel);

            if (!(classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax syntax))
                return;

            foreach (var member in syntax.Members)
            {
                if (member is FieldDeclarationSyntax field)
                {
                    if (!field.HasAttribute("NetworkVariable"))
                        continue;

                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (!GetNetworkVariableId(field, out byte id))
                            continue;

                        string fieldName = variable.Identifier.Text;
                        if (networkVariables.TryGetValue(id, out var existing))
                        {
                            context.ReportDiagnostic(DuplicateNetworkVariableId, variable.GetLocation(), id.ToString(), $"{existing.ClassName}.{existing.FieldName}");
                            continue;
                        }

                        networkVariables[id] = (fieldName, variable.GetLocation(), classSymbol.Name);
                    }
                }
            }
        }

        private bool GetNetworkVariableId(FieldDeclarationSyntax member, out byte id)
        {
            AttributeSyntax attribute = member.GetAttribute("NetworkVariable");
            if (attribute != null)
            {
                var idExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("id", ArgumentIndex.First);
                if (idExpression != null)
                {
                    if (byte.TryParse(idExpression.Token.ValueText, out byte idValue))
                    {
                        id = idValue;
                        return true;
                    }
                }
            }

            id = 0;
            return false;
        }
    }

}