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
						return Helpers.CreateClass("public partial", @class, "NetworkBehaviour", OnCreated: () =>
						{
							StringBuilder methodBuilder = new StringBuilder();
							IEnumerable<AttributeWithMultipleParameters> attributes = classSyntax.GetAttributesWithMultipleParameters(context.GetSemanticModel(classSyntax.SyntaxTree), "Remote");
							foreach (AttributeWithMultipleParameters attribute in attributes)
							{
								try
								{
									var idParameter = attribute.ParametersByName["Id"];
									var nameParameter = attribute.ParametersByName["Name"];
									var selfParameter = attribute.ParametersByName["Self"];
									if (idParameter.Value != null && nameParameter.Value != null && selfParameter.Value != null)
									{
										bool isSelf = bool.Parse(selfParameter.Value);
										if (isSelf)
										{
											methodBuilder.AppendLine("");
											methodBuilder.AppendLine($"\t\t[Remote(Id = {idParameter.Value})]");
											methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}(IDataReader reader, NetworkPeer peer);");
										}
										else
										{
											methodBuilder.AppendLine("");
											methodBuilder.AppendLine($"\t\t[Remote(Id = {idParameter.Value})]");
											methodBuilder.AppendLine($"\t\tprivate void zzzz{nameParameter.Value}zzzz(IDataReader reader, NetworkPeer peer)");
											methodBuilder.AppendLine("\t\t{");
											methodBuilder.AppendLine($"\t\t\tif (IsServer) {nameParameter.Value}_Server(reader, peer);");
											methodBuilder.AppendLine($"\t\t\telse {nameParameter.Value}_Client(reader, peer);");
											methodBuilder.AppendLine("\t\t}");
											// Server Method
											methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}_Server(IDataReader reader, NetworkPeer peer);");
											// Client Method
											methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}_Client(IDataReader reader, NetworkPeer peer);");
										}
									}
								}
								catch
								{
									continue;
								}
							}
							return methodBuilder.ToString();
						});
					}));
					context.AddSource($"{@class}_g", builder.ToString().Trim());
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