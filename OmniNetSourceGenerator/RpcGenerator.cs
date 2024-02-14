using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class RpcGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is RpcSyntaxReceiver rpcSyntaxReceiver)
			{
				foreach (var classSyntax in rpcSyntaxReceiver.ClassDeclarationSyntaxes)
				{
					StringBuilder builder = new StringBuilder();
					string @class = classSyntax.GetIdentifierName();
					builder.AppendLine(Helpers.CreateNamespace(classSyntax.GetNamespaceName(), new List<string> { "using Omni.Core;" }, () =>
					{
						return Helpers.CreateClass("public partial", @class, "NetworkBehaviour", onClassCreated: () =>
						{
							StringBuilder methodBuilder = new StringBuilder();
							var attributes = classSyntax.GetAttributeParametersWithInfo(context.GetSemanticModel(classSyntax.SyntaxTree), "Remote");
							foreach ((string name, string value, string type) in attributes)
							{
								methodBuilder.AppendLine("//" + name + " : " + value + " : " + type);
							}
							return methodBuilder.ToString();
						});
					}));
					context.AddSource($"{@class}_g", builder.ToString());
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new RpcSyntaxReceiver());
		}

		internal class RpcSyntaxReceiver : ISyntaxReceiver
		{
			public List<ClassDeclarationSyntax> ClassDeclarationSyntaxes { get; } = new List<ClassDeclarationSyntax>();
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode.GetDeclarationSyntax(out ClassDeclarationSyntax classDeclarationSyntax))
				{
					if (classDeclarationSyntax.HasModifier(SyntaxKind.PartialKeyword))
					{
						if (classDeclarationSyntax.HasAttribute("Remote"))
						{
							ClassDeclarationSyntaxes.Add(classDeclarationSyntax);
						}
					}
				}
			}
		}
	}
}