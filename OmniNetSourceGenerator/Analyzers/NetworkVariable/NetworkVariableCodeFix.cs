using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
}