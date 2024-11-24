using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class NetworkServiceInjectionGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is ServiceInjectionSyntaxReceiver syntaxReceiver)
			{
				if (syntaxReceiver.members.Any())
				{
					IEnumerable<ClassStructure> classes = syntaxReceiver.members.GroupMembersByParentClass();
					foreach (ClassStructure @class in classes)
					{
						StringBuilder sb = new StringBuilder();
						sb.AppendLine("#nullable disable");
						sb.AppendLine("#pragma warning disable");
						sb.AppendLine("using Omni.Core;");
						sb.AppendLine();

						ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
						foreach (UsingDirectiveSyntax usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
							sb.AppendLine(usingSyntax.ToString());

						if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
						{
							if (parentClass.HasBaseType("NetworkBehaviour", "DualBehaviour", "ClientBehaviour", "ServerBehaviour", "ServiceBehaviour", "Base"))
							{
								NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
								if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

								string baseClassName = parentClass.GetBaseTypeName();
								List<StatementSyntax> statements = new List<StatementSyntax>();
								foreach (MemberDeclarationSyntax member in @class.Members)
								{
									string serviceName = "";
									IEnumerable<AttributeSyntax> attributes = member.GetAttributes("GlobalService", "LocalService");
									if (attributes.Any())
									{
										foreach (var attribute in attributes)
										{
											var arguments = attribute.ArgumentList.Arguments;
											var nameTypeExpression = GenHelper.GetArgumentExpression<LiteralExpressionSyntax>("ServiceName", 0, arguments);
											if (nameTypeExpression != null)
											{
												serviceName = nameTypeExpression.Token.ValueText;
											}
										}
									}

									bool isGlobalService = member.HasAttribute("GlobalService");
									if (member is FieldDeclarationSyntax field)
									{
										foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
										{
											AddStatement(context, statements, baseClassName, serviceName, isGlobalService, variable.Identifier.Text, field.Declaration.Type);
										}
									}
									else if (member is PropertyDeclarationSyntax property)
									{
										AddStatement(context, statements, baseClassName, serviceName, isGlobalService, property.Identifier.Text, property.Type);
									}
								}

								statements.Add(SyntaxFactory.ParseStatement("base.___InjectServices___();"));

								MethodDeclarationSyntax injectServicesMethod = OverrideInjectServicesMethod()
									.WithBody(SyntaxFactory.Block(statements));

								parentClass = parentClass.AddMembers(injectServicesMethod);

								if (!hasNamespace)
								{
									sb.AppendLine("// Generated by OmniNetSourceGenerator");
									sb.Append(parentClass.NormalizeWhitespace().ToString());
								}
								else
								{
									currentNamespace = currentNamespace.AddMembers(parentClass);
									sb.AppendLine("// Generated by OmniNetSourceGenerator");
									sb.Append(currentNamespace.NormalizeWhitespace().ToString());
								}

								context.AddSource($"{parentClass.Identifier.ToFullString()}_networkservice_injection_generated_code.cs", sb.ToString());
							}
							else
							{
								GenHelper.ReportInheritanceRequirement(context);
							}
						}
						else
						{
							GenHelper.ReportPartialKeywordRequirement(context);
						}
					}
				}
			}
		}

		private void AddStatement(
			GeneratorExecutionContext context,
			List<StatementSyntax> statements,
			string baseClassName,
			string serviceName,
			bool isGlobalService,
			string memberName,
			TypeSyntax typeSyntax)
		{
			if (isGlobalService)
			{
				AddGlobalServiceStatement(statements, serviceName, memberName, typeSyntax);
			}
			else
			{
				AddLocalServiceStatement(context, statements, baseClassName, serviceName, memberName, typeSyntax);
			}
		}

		private void AddGlobalServiceStatement(
			List<StatementSyntax> statements,
			string serviceName,
			string memberName,
			TypeSyntax typeSyntax)
		{
			string statement = string.IsNullOrEmpty(serviceName)
				? $"{memberName} = NetworkService.Get<{typeSyntax}>();"
				: $"{memberName} = NetworkService.Get<{typeSyntax}>(\"{serviceName}\");";

			statements.Add(SyntaxFactory.ParseStatement(statement));
		}

		private void AddLocalServiceStatement(
			GeneratorExecutionContext context,
			List<StatementSyntax> statements,
			string baseClassName,
			string serviceName,
			string memberName,
			TypeSyntax typeSyntax)
		{
			if (baseClassName == "NetworkBehaviour" || baseClassName.Contains("Base"))
			{
				string statement = string.IsNullOrEmpty(serviceName)
					? $"{memberName} = Identity.Get<{typeSyntax}>();"
					: $"{memberName} = Identity.Get<{typeSyntax}>(\"{serviceName}\");";

				statements.Add(SyntaxFactory.ParseStatement(statement));
			}
			else
			{
				ReportInvalidBaseClassForLocalService(context);
			}
		}

		private void ReportInvalidBaseClassForLocalService(GeneratorExecutionContext context)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					new DiagnosticDescriptor(
						"OMNI013",
						"Invalid Base Class for LocalService",
						"The 'LocalService' attribute can only be applied to classes that inherit from 'NetworkBehaviour' or a derived class. Please update the base class to comply with this requirement.",
						"Design",
						DiagnosticSeverity.Error,
						isEnabledByDefault: true
					),
					Location.None
				)
			);
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new ServiceInjectionSyntaxReceiver());
		}

		private MethodDeclarationSyntax OverrideInjectServicesMethod()
		{
			return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "___InjectServices___")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)));
		}
	}

	internal class ServiceInjectionSyntaxReceiver : ISyntaxReceiver
	{
		internal List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is MemberDeclarationSyntax memberSyntax)
			{
				if (memberSyntax.HasAttribute("GlobalService", "LocalService"))
				{
					members.Add(memberSyntax);
				}
			}
		}
	}
}