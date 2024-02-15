using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SourceGenerator.Generators
{
	[Generator]
	internal class SyncVarGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is SyncVarSyntaxReceiver rpcSyntaxReceiver)
				{
					foreach (var fieldSyntax in rpcSyntaxReceiver.FieldDeclarationSyntaxes)
					{
						StringBuilder builder = new StringBuilder();
						string @class = fieldSyntax.ClassDeclarationSyntax.GetClassName();
						string @namespace = fieldSyntax.NamespaceDeclarationSyntax.GetNamespaceName();
						builder.AppendLine(Helpers.CreateNamespace(@namespace, new List<string> { "using Omni.Core;" }, () =>
						{
							return Helpers.CreateClass("public partial", @class, "NetworkBehaviour", OnCreated: () =>
							{
								return "// Boa!";
							});
						}));
						context.AddSource($"syncvar_g", builder.ToString().Trim());
					}
				}
			}
			catch (Exception ex)
			{
				Helpers.Log("SyncVarGen", ex.ToString());
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyncVarSyntaxReceiver());
		}

		internal class SyncVarSyntaxReceiver : ISyntaxReceiver
		{
			internal class OmniFieldDeclarationSyntax
			{
				public OmniFieldDeclarationSyntax(FieldDeclarationSyntax fieldDeclarationSyntax, NamespaceDeclarationSyntax namespaceDeclarationSyntax, ClassDeclarationSyntax classDeclarationSyntax)
				{
					FieldDeclarationSyntax = fieldDeclarationSyntax;
					NamespaceDeclarationSyntax = namespaceDeclarationSyntax;
					ClassDeclarationSyntax = classDeclarationSyntax;
				}

				internal FieldDeclarationSyntax FieldDeclarationSyntax { get; }
				internal NamespaceDeclarationSyntax NamespaceDeclarationSyntax { get; }
				internal ClassDeclarationSyntax ClassDeclarationSyntax { get; }
			}

			public List<OmniFieldDeclarationSyntax> FieldDeclarationSyntaxes { get; } = new List<OmniFieldDeclarationSyntax>();
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode.TryGetDeclarationSyntax(out FieldDeclarationSyntax fieldDeclarationSyntax))
				{
					if (fieldDeclarationSyntax.HasAttribute("SyncVar"))
					{
						ClassDeclarationSyntax classDeclarationSyntax = fieldDeclarationSyntax.GetClassDeclarationSyntax();
						if (classDeclarationSyntax.HasModifier(SyntaxKind.PartialKeyword))
						{
							FieldDeclarationSyntaxes.Add(new OmniFieldDeclarationSyntax(fieldDeclarationSyntax, fieldDeclarationSyntax.GetNamespaceDeclarationSyntax(), classDeclarationSyntax));
						}
					}
				}
			}
		}
	}
}
