using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
				if (context.SyntaxReceiver is SyncVarSyntaxReceiver syncVarSyntaxReceiver)
				{
					if (syncVarSyntaxReceiver.FieldDeclarationSyntaxes.Any())
					{
						var classGroups = syncVarSyntaxReceiver.FieldDeclarationSyntaxes.GroupBy(x => x.ClassDeclarationSyntax);
						foreach (var classGroup in classGroups)
						{
							ClassDeclarationSyntax classDeclarationSyntax = classGroup.Key;
							StringBuilder classBuilder = new StringBuilder();
							StringBuilder fieldBuilder = new StringBuilder();
							string @class = classDeclarationSyntax.GetClassName();
							string @namespace = classDeclarationSyntax.GetNamespaceName();
							foreach (var fieldDeclarationSyntax in classGroup)
							{
								var fieldSyntaxes = fieldDeclarationSyntax.FieldDeclarationSyntax.GetFieldVariables();
								foreach (var fieldSyntax in fieldSyntaxes)
								{
									MemberInfo fieldInfo = fieldSyntax.GetFieldInfo(context.GetSemanticModel(fieldSyntax.SyntaxTree));
									string fieldName = fieldInfo.Name.Replace("m_", "");
									string propertyName = $"{char.ToUpperInvariant(fieldName[0])}{fieldName.Substring(1)}";
									fieldBuilder.AppendLine("");
									fieldBuilder.AppendLine($"\t\tprivate {fieldInfo.TypeName} {propertyName}");
									fieldBuilder.AppendLine("\t\t{");
									fieldBuilder.AppendLine("\t\t\tget");
									fieldBuilder.AppendLine("\t\t\t{");
									fieldBuilder.AppendLine($"\t\t\t\t return {fieldInfo.Name};");
									fieldBuilder.AppendLine("\t\t\t}");
									fieldBuilder.AppendLine("\t\t\tset");
									fieldBuilder.AppendLine("\t\t\t{");
									fieldBuilder.AppendLine("\t\t\t}");
									fieldBuilder.AppendLine("\t\t}");
								}
							}

							var usings = classDeclarationSyntax.GetAllUsingsDirective().Select(x => $"using {x.Name};");
							classBuilder.AppendLine(Helpers.CreateNamespace(@namespace, usings, () =>
							{
								return Helpers.CreateClass("partial", @class, "NetworkBehaviour", OnCreated: () =>
								{
									return fieldBuilder.ToString();
								});
							}));
							context.AddSource($"{@class}__syncvar_g", classBuilder.ToString().Trim());
						}
					}


					//foreach (var fieldDeclarationSyntax in rpcSyntaxReceiver.FieldDeclarationSyntaxes)
					//{
					//	StringBuilder builder = new StringBuilder();
					//	string @class = fieldDeclarationSyntax.ClassDeclarationSyntax.GetClassName();
					//	string @namespace = fieldDeclarationSyntax.NamespaceDeclarationSyntax.GetNamespaceName();
					//	builder.AppendLine(Helpers.CreateNamespace(@namespace, new List<string> { "using Omni.Core;" }, () =>
					//	{
					//		return Helpers.CreateClass("partial", @class, "NetworkBehaviour", OnCreated: () =>
					//		{
					//			StringBuilder fieldBuilder = new StringBuilder();
					//			var fieldSyntaxes = fieldDeclarationSyntax.FieldDeclarationSyntax.GetFieldVariables();
					//			foreach (var fieldSyntax in fieldSyntaxes)
					//			{
					//				MemberInfo fieldInfo = fieldSyntax.GetFieldInfo(context.GetSemanticModel(fieldSyntax.SyntaxTree));
					//				string fieldName = fieldInfo.Name.Replace("m_", "");
					//				string propertyName = $"{char.ToUpperInvariant(fieldName[0])}{fieldName.Substring(1)}";
					//				fieldBuilder.AppendLine("");
					//				fieldBuilder.AppendLine($"\t\tprivate {fieldInfo.TypeName} {propertyName}");
					//				fieldBuilder.AppendLine("\t\t{");
					//				fieldBuilder.AppendLine("\t\t\tget");
					//				fieldBuilder.AppendLine("\t\t\t{");
					//				fieldBuilder.AppendLine($"\t\t\t\t return {fieldInfo.Name};");
					//				fieldBuilder.AppendLine("\t\t\t}");
					//				fieldBuilder.AppendLine("\t\t\tset");
					//				fieldBuilder.AppendLine("\t\t\t{");
					//				fieldBuilder.AppendLine("\t\t\t}");
					//				fieldBuilder.AppendLine("\t\t}");
					//			}
					//			return fieldBuilder.ToString();
					//		});
					//	}));
					//	context.AddSource($"{@class}__syncvar_g", builder.ToString().Trim());
					//}
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
					if (fieldDeclarationSyntax.HasAttribute("NetVar"))
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
