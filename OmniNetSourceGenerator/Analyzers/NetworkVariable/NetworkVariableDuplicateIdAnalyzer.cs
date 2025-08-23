using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkVariableDuplicateIdAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor DuplicateNetworkVariableId = new DiagnosticDescriptor(
            id: "OMNI019",
            title: "Duplicate Network Variable ID",
            messageFormat: "The network variable with Id {0} is already defined in {1}",
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

                var variables = new Dictionary<byte, (string FieldName, Location Location, string ClassName)>();
                CollectNetworkVariables(new Context(context), classSymbol, variables, semanticModel);
            }
        }

        private void CollectNetworkVariables(
            Context context,
            INamedTypeSymbol classSymbol,
            Dictionary<byte, (string fieldName, Location location, string className)> variables,
            SemanticModel semanticModel)
        {
            if (classSymbol.BaseType != null)
                CollectNetworkVariables(context, classSymbol.BaseType, variables, semanticModel);

            if (!(classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax syntax))
                return;

            foreach (var member in syntax.Members)
            {
                if (member is FieldDeclarationSyntax field)
                {
                    if (!field.HasAttribute("NetworkVariable"))
                        continue;

                    if (!GetNetworkVariableId(field, context.SyntaxNodeAnalysisContext.Value.SemanticModel, out byte currentId))
                        continue;

                    foreach (var variable in field.Declaration.Variables)
                    {
                        string fieldName = variable.Identifier.Text;
                        if (variables.TryGetValue(currentId, out var existing))
                        {
                            context.ReportDiagnostic(DuplicateNetworkVariableId, variable.GetLocation(), currentId.ToString(), $"{existing.className}.{existing.fieldName}");
                            continue;
                        }

                        variables[currentId] = (fieldName, variable.GetLocation(), classSymbol.Name);
                    }
                }
            }
        }

        private bool GetNetworkVariableId(FieldDeclarationSyntax member, SemanticModel semanticModel, out byte id)
        {
            AttributeSyntax attribute = member.GetAttribute("NetworkVariable");
            if (attribute != null)
            {
                id = attribute.GetArgumentValue<byte>("id", ArgumentIndex.First, semanticModel, 0);
                return id > 0;
            }

            id = 0;
            return false;
        }
    }

}