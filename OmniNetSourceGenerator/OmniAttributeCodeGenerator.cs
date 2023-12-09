using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class OmniAttributeCodeGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is OmniSyntaxReceiver omniSyntaxReceiver)
			{
				foreach (ClassDeclarationSyntax classDeclarationSyntax in omniSyntaxReceiver.ClassDeclarationSyntaxes)
				{
					StringBuilder builder = new StringBuilder();
					string @class = classDeclarationSyntax.GetIdentifierName();
					string @class_g = $"{@class}_g";
					string @namespace = classDeclarationSyntax.GetNamespaceName();

					builder.AppendLine(Helpers.CreateNamespace("Omni.Core.Generated", new List<string> { "using Omni.Core.Generated;" }, () =>
					{
						return Helpers.CreateClass("public", @class_g, onClassCreated: () => $"// ruan kkkk");
					}));

					builder.AppendLine(Helpers.CreateNamespace(@namespace, () =>
					{
						return Helpers.CreateClass("public partial", @class, @class_g, () => $"// ruan kkkk");
					}));
					context.AddSource(@class_g, builder.ToString());
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new OmniSyntaxReceiver());
		}
	}

	internal class OmniSyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> ClassDeclarationSyntaxes { get; } = new List<ClassDeclarationSyntax>();
		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode.GetDeclarationSyntax(out ClassDeclarationSyntax classDeclarationSyntax))
			{
				if (classDeclarationSyntax.HasModifier(SyntaxKind.PartialKeyword))
				{
					if (classDeclarationSyntax.HasAttribute("Omni"))
					{
						ClassDeclarationSyntaxes.Add(classDeclarationSyntax);
					}
				}
			}
		}
	}
}
