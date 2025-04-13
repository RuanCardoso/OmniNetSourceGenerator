using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using System.Collections.Immutable;
using System.Linq;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DataBufferDisposeAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DataBufferDisposeSuggestion = new DiagnosticDescriptor(
            id: "OMNI061",
            title: "Use Dispose() or Using Block With Rented DataBuffer",
            messageFormat: "DataBuffer obtained via Rent() should be disposed using Dispose() or a using block",
            category: "Resource Management",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "DataBuffer instances obtained from the pool via Rent() must be returned to the pool with Dispose() " +
                         "to prevent memory leaks and ensure proper resource management. Use either Dispose() explicitly " +
                         "or a using block/declaration to guarantee proper cleanup."
        );

        public static readonly DiagnosticDescriptor UnnecessaryDisposeWarning = new DiagnosticDescriptor(
            id: "OMNI062",
            title: "Unnecessary Dispose For Non-Rented DataBuffer",
            messageFormat: "DataBuffer created with 'new' should not be disposed with Dispose() or a using block",
            category: "Resource Management",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "DataBuffer instances created with 'new' don't need to be disposed with Dispose() or a using block. " +
                         "Only DataBuffer instances obtained via Rent() need to be returned to the pool."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DataBufferDisposeSuggestion, UnnecessaryDisposeWarning);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register a syntax node action for invocation expressions (to detect Rent() calls)
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);

            // Register for object creation expressions (to detect new DataBuffer())
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

            // Register for implicit object creation expressions (to detect new())
            context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocationExpression))
                return;

            // Check if we're inside a network class
            if (!IsInsideNetworkClass(invocationExpression, context.SemanticModel))
                return;

            // Check if this is a call to a Rent() method returning DataBuffer
            if (!IsRentMethodReturningDataBuffer(invocationExpression, context.SemanticModel))
                return;

            // Get the variable declaration or assignment that stores the rented buffer
            var variableSymbol = GetVariableStoringRentedBuffer(invocationExpression, context.SemanticModel);
            if (variableSymbol == null)
                return;

            // Check if the buffer is properly disposed
            if (!IsProperlyDisposed(variableSymbol, invocationExpression, context.SemanticModel))
            {
                // Report diagnostic for rented DataBuffer not being disposed
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DataBufferDisposeSuggestion,
                        invocationExpression.GetLocation()
                    )
                );
            }
        }

        private void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ImplicitObjectCreationExpressionSyntax implicitObjectCreation))
                return;

            // Check if we're inside a network class
            if (!IsInsideNetworkClass(implicitObjectCreation, context.SemanticModel))
                return;

            // Get semantic model and type information
            var semanticModel = context.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(implicitObjectCreation);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null || typeSymbol.Name != "DataBuffer")
                return;

            // Get the variable storing the new DataBuffer
            var variableSymbol = GetVariableStoringCreatedBuffer(implicitObjectCreation, semanticModel);
            if (variableSymbol == null)
                return;

            // Check if the buffer is being unnecessarily disposed
            if (IsProperlyDisposed(variableSymbol, implicitObjectCreation, semanticModel))
            {
                // Report diagnostic for unnecessarily disposing a new DataBuffer
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        UnnecessaryDisposeWarning,
                        implicitObjectCreation.GetLocation()
                    )
                );
            }
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ObjectCreationExpressionSyntax objectCreation))
                return;

            // Check if we're inside a network class
            if (!IsInsideNetworkClass(objectCreation, context.SemanticModel))
                return;

            // Get semantic model and type information
            var semanticModel = context.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(objectCreation);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null || typeSymbol.Name != "DataBuffer")
                return;

            // Get the variable storing the new DataBuffer
            var variableSymbol = GetVariableStoringCreatedBuffer(objectCreation, semanticModel);
            if (variableSymbol == null)
                return;

            // Check if the buffer is being unnecessarily disposed
            if (IsProperlyDisposed(variableSymbol, objectCreation, semanticModel))
            {
                // Report diagnostic for unnecessarily disposing a new DataBuffer
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        UnnecessaryDisposeWarning,
                        objectCreation.GetLocation()
                    )
                );
            }
        }

        private bool IsInsideNetworkClass(SyntaxNode node, SemanticModel semanticModel)
        {
            // Find the containing class declaration
            var containingClass = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (containingClass == null)
                return false;

            // Check if this class inherits from one of the network-related base classes
            return containingClass.InheritsFromClass(semanticModel,
                "NetworkBehaviour", "DualBehaviour", "ClientBehaviour", "ServerBehaviour");
        }

        private bool IsRentMethodReturningDataBuffer(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null || methodSymbol.Name != "Rent")
                return false;

            // Check if the return type is DataBuffer
            var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
            return returnType != null && returnType.Name == "DataBuffer";
        }

        private ISymbol GetVariableStoringRentedBuffer(InvocationExpressionSyntax rentCall, SemanticModel semanticModel)
        {
            return GetVariableStoringExpression(rentCall, semanticModel);
        }

        private ISymbol GetVariableStoringCreatedBuffer(SyntaxNode objectCreation, SemanticModel semanticModel)
        {
            return GetVariableStoringExpression(objectCreation, semanticModel);
        }

        private ISymbol GetVariableStoringExpression(SyntaxNode expression, SemanticModel semanticModel)
        {
            // Check if the expression is part of a variable declaration
            var variableDeclaration = expression.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            if (variableDeclaration != null)
            {
                var declarator = variableDeclaration.Variables.FirstOrDefault(v => v.Initializer?.Value == expression);
                if (declarator != null)
                {
                    return semanticModel.GetDeclaredSymbol(declarator);
                }
            }

            // Check if it's part of an assignment expression
            var assignment = expression.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
            if (assignment != null && assignment.Right == expression)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(assignment.Left);
                return symbolInfo.Symbol;
            }

            return null;
        }

        private bool IsProperlyDisposed(ISymbol variableSymbol, SyntaxNode node, SemanticModel semanticModel)
        {
            // Get containing method or lambda or local function
            var methodBlock = node.Ancestors().OfType<BlockSyntax>().FirstOrDefault(
                b => b.Parent is MethodDeclarationSyntax ||
                     b.Parent is LambdaExpressionSyntax ||
                     b.Parent is LocalFunctionStatementSyntax);

            if (methodBlock == null)
                return false;

            // Check if variable is used in a using statement or using declaration
            var usings = methodBlock.DescendantNodes().OfType<UsingStatementSyntax>()
                .Where(u => IsVariableInUsing(u, variableSymbol, semanticModel))
                .Any();

            if (usings)
                return true;

            var usingDeclarations = methodBlock.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                .Where(l => l.UsingKeyword != default && IsVariableInUsingDeclaration(l, variableSymbol, semanticModel))
                .Any();

            if (usingDeclarations)
                return true;

            // Check for direct Dispose() calls on the variable
            var disposeInvocations = methodBlock.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(i => IsDisposeCallOnVariable(i, variableSymbol, semanticModel))
                .Any();

            return disposeInvocations;
        }

        private bool IsVariableInUsing(UsingStatementSyntax usingStatement, ISymbol variableSymbol, SemanticModel semanticModel)
        {
            if (usingStatement.Declaration != null)
            {
                // Using with declaration: using (var buffer = DataBuffer.Rent())
                foreach (var variable in usingStatement.Declaration.Variables)
                {
                    var declaredSymbol = semanticModel.GetDeclaredSymbol(variable);
                    if (declaredSymbol != null && SymbolEqualityComparer.Default.Equals(declaredSymbol, variableSymbol))
                        return true;
                }
            }
            else if (usingStatement.Expression != null)
            {
                // Using with expression: using (buffer)
                var symbolInfo = semanticModel.GetSymbolInfo(usingStatement.Expression);
                return symbolInfo.Symbol != null && SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, variableSymbol);
            }

            return false;
        }

        private bool IsVariableInUsingDeclaration(LocalDeclarationStatementSyntax declaration, ISymbol variableSymbol, SemanticModel semanticModel)
        {
            // Using declaration: using var buffer = DataBuffer.Rent();
            foreach (var variable in declaration.Declaration.Variables)
            {
                var declaredSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (declaredSymbol != null && SymbolEqualityComparer.Default.Equals(declaredSymbol, variableSymbol))
                    return true;
            }

            return false;
        }

        private bool IsDisposeCallOnVariable(InvocationExpressionSyntax invocation, ISymbol variableSymbol, SemanticModel semanticModel)
        {
            // Check for explicit dispose: buffer.Dispose()
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "Dispose")
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
                return symbolInfo.Symbol != null && SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, variableSymbol);
            }

            return false;
        }
    }
}