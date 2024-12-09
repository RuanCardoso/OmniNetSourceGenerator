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
	class InvokeCallMethodGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is InvokeCallMethodSyntaxReceiver syntaxReceiver)
			{
				if (syntaxReceiver.methods.Any())
				{
					IEnumerable<ClassStructure> classes = syntaxReceiver.methods.GroupMembersByParentClass();
					foreach (ClassStructure @class in classes)
					{
						StringBuilder sb = new StringBuilder();
						sb.AppendLine("#nullable disable");
						sb.AppendLine("#pragma warning disable");
						sb.AppendLine();

						ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
						foreach (UsingDirectiveSyntax usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
							sb.AppendLine(usingSyntax.ToString());

						if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
						{
							var semanticModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
							bool isNetworkBehaviour = fromClass.InheritsFrom(semanticModel, "NetworkBehaviour");
							bool isNonNetworkBehaviour = fromClass.InheritsFrom(semanticModel, "DualBehaviour", "ClientBehaviour", "ServerBehaviour");
							if (isNetworkBehaviour || isNonNetworkBehaviour)
							{
								NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
								if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

								List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();
								foreach (MethodDeclarationSyntax method in @class.Members.Cast<MethodDeclarationSyntax>())
								{
									byte id = 0;
									bool isServerAttribute = method.HasAttribute("Server");

									IEnumerable<AttributeSyntax> attributes = method.GetAttributes("Server", "Client");
									if (attributes.Any())
									{
										foreach (var attributeSyntax in attributes)
										{
											var arguments = attributeSyntax.ArgumentList.Arguments;
											var idTypeExpression = GenHelper.GetArgumentExpression<LiteralExpressionSyntax>("id", 0, arguments);
											if (idTypeExpression != null)
											{
												if (byte.TryParse(idTypeExpression.Token.ValueText, out byte idValue))
												{
													id = idValue;
												}
											}
										}
									}

									methods.Add(
										SyntaxFactory.MethodDeclaration(method.ReturnType, method.Identifier)
										.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
										.WithParameterList(SyntaxFactory.ParameterList(
											SyntaxFactory.SeparatedList(
												isNetworkBehaviour ? new ParameterSyntax[] {
												isServerAttribute ? SyntaxFactory.Parameter(SyntaxFactory.Identifier("ClientOptions options")) : SyntaxFactory.Parameter(SyntaxFactory.Identifier("ServerOptions options"))
											} : isNonNetworkBehaviour ? new ParameterSyntax[] {
												SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer")),
												isServerAttribute ? SyntaxFactory.Parameter(SyntaxFactory.Identifier("ClientOptions options")) : SyntaxFactory.Parameter(SyntaxFactory.Identifier("ServerOptions options"))
											} : null))
										).WithBody(SyntaxFactory.Block(isServerAttribute
										? isNetworkBehaviour ? SyntaxFactory.ParseStatement($"Local.Invoke({id}, options);") : isNonNetworkBehaviour ? SyntaxFactory.ParseStatement($"Local.Invoke({id}, options);") : null
										: isNetworkBehaviour ? SyntaxFactory.ParseStatement($"Remote.Invoke({id}, options);") : isNonNetworkBehaviour ? SyntaxFactory.ParseStatement($"Remote.Invoke({id}, peer, options);") : null))
										.WithLeadingTrivia(new SyntaxTrivia[] {
											SyntaxFactory.Comment("/// <summary>"),
											SyntaxFactory.Comment($"/// Executes the remote procedure call (RPC) '{method.Identifier.Text}' on the {(isServerAttribute ? "'Server'" : "'Client'")}, called by the {(isServerAttribute ? "'Client'" : "'Server'")}.<br/>"),
											SyntaxFactory.Comment($"/// It uses the specified <see cref=\"SyncOptions\" /> to define the synchronization parameters."),
											SyntaxFactory.Comment("/// </summary>"),
											SyntaxFactory.Comment("/// <param name=\"options\">"),
											SyntaxFactory.Comment($"/// Defines the synchronization options for this RPC call. These options include"),
											SyntaxFactory.Comment($"/// settings for target, delivery mode, and others."),
											SyntaxFactory.Comment("/// </param>"),
											SyntaxFactory.Comment("/// <remarks>"),
											SyntaxFactory.Comment($"/// This method is auto-generated and should not be modified manually."),
											SyntaxFactory.Comment("/// </remarks>")
										})
									);
								}

								parentClass = parentClass.AddMembers(methods.ToArray());

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

								context.AddSource($"{parentClass.Identifier.ToFullString()}_invoke_call_method_generated_code.cs", sb.ToString());
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

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new InvokeCallMethodSyntaxReceiver());
		}
	}

	internal class InvokeCallMethodSyntaxReceiver : ISyntaxReceiver
	{
		internal List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is MethodDeclarationSyntax memberSyntax)
			{
				if (memberSyntax.HasAttribute("Server", "Client"))
				{
					methods.Add(memberSyntax);
				}
			}
		}
	}
}
