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
	internal class RpcGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is RpcSyntaxReceiver rpcSyntaxReceiver)
				{
					foreach (var classSyntax in rpcSyntaxReceiver.ClassDeclarationSyntaxes)
					{
						StringBuilder builder = new StringBuilder();
						string @class = classSyntax.GetClassName();
						var usings = classSyntax.GetAllUsingsDirective().Select(x => $"using {x.Name};");
						builder.AppendLine(Helpers.CreateNamespace(classSyntax.GetNamespaceName(), usings, () =>
						{
							return Helpers.CreateClass("partial", @class, "NetworkBehaviour", OnCreated: () =>
							{
								StringBuilder methodBuilder = new StringBuilder();
								IEnumerable<AttributesWithMultipleParameters> attributes = classSyntax.GetAttributesWithMultipleParameters(context.GetSemanticModel(classSyntax.SyntaxTree), "Remote");
								if (attributes != null && attributes.Any())
								{
									foreach (AttributesWithMultipleParameters attribute in attributes)
									{
										try
										{
											continue;
											if (attribute.ParametersByName.Count == 3)
											{
												var idParameter = attribute.ParametersByName["Id"]; // 1 required
												var nameParameter = attribute.ParametersByName["Name"];  // 2 required
												var selfParameter = attribute.ParametersByName["Self"]; //  3 required
												if (idParameter.Value != null && nameParameter.Value != null && selfParameter.Value != null)
												{
													if (bool.TryParse(selfParameter.Value, out bool itSelf))
													{
														if (itSelf)
														{
															methodBuilder.AppendLine("");
															methodBuilder.AppendLine($"\t\t[Remote(Id = {idParameter.Value})]");
															methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}(IDataReader reader, NetworkPeer peer);");
														}
														else
														{
															methodBuilder.AppendLine("");
															methodBuilder.AppendLine($"\t\t[Remote(Id = {idParameter.Value})]");
															methodBuilder.AppendLine($"\t\tprivate void _{Math.Abs(nameParameter.Value.GetHashCode())}(IDataReader reader, NetworkPeer peer)"); // Hide method!
															methodBuilder.AppendLine("\t\t{");
															methodBuilder.AppendLine($"\t\t\tif (IsServer) {nameParameter.Value}_Server(reader, peer);");
															methodBuilder.AppendLine($"\t\t\telse {nameParameter.Value}_Client(reader, peer);");
															methodBuilder.AppendLine("\t\t}");
															// Server Method
															methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}_Server(IDataReader reader, NetworkPeer peer);");
															// Client Method
															methodBuilder.AppendLine($"\t\tpartial void {nameParameter.Value}_Client(IDataReader reader, NetworkPeer peer);");
														}

														methodBuilder.AppendLine($"\t\tprivate byte {nameParameter.Value}_Id => {idParameter.Value};");
														// Send to Client
														methodBuilder.AppendLine($"\t\tprivate void {nameParameter.Value}(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int playerId, byte channel = 0)");
														methodBuilder.AppendLine("\t\t{");
														methodBuilder.AppendLine($"\t\t\tRpc(writer, dataDeliveryMode, playerId, {nameParameter.Value}_Id, channel);");
														methodBuilder.AppendLine("\t\t}");

														// Send to Server
														methodBuilder.AppendLine($"\t\tprivate void {nameParameter.Value}(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte channel = 0)");
														methodBuilder.AppendLine("\t\t{");
														methodBuilder.AppendLine($"\t\t\tRpc(writer, dataDeliveryMode, {nameParameter.Value}_Id, channel);");
														methodBuilder.AppendLine("\t\t}");
													}
												}
											}
										}
										catch (Exception ex)
										{
											Helpers.Log("RpcGen", ex.ToString());
											continue;
										}
									}
								}
								return methodBuilder.ToString();
							});
						}));
						context.AddSource($"{@class}__rpc_g", builder.ToString().Trim());
					}
				}
			}
			catch (Exception ex)
			{
				Helpers.Log("RpcGen", ex.ToString());
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
				if (syntaxNode.TryGetDeclarationSyntax(out ClassDeclarationSyntax classDeclarationSyntax))
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