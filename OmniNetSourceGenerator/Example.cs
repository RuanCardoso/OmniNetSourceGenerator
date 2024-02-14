//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using System.Collections.Generic;
//using System.Text;

//namespace OmniNetSourceGenerator
//{
//	//[Generator]
//	internal class Example : ISourceGenerator
//	{
//		public void Execute(GeneratorExecutionContext context)
//		{
//			if (context.SyntaxReceiver is OmniSyntaxReceiver omniSyntaxReceiver)
//			{
//				foreach (ClassDeclarationSyntax classDeclarationSyntax in omniSyntaxReceiver.ClassDeclarationSyntaxes)
//				{
//					StringBuilder builder = new StringBuilder();
//					string @class = classDeclarationSyntax.GetIdentifierName();
//					string @class_g = $"{@class}_g";
//					string @namespace = classDeclarationSyntax.GetNamespaceName();

//					builder.AppendLine(Helpers.CreateNamespace("Omni.Core.Generated", new List<string> { "using Omni.Core;", "using Omni.Core.Generated;" }, () =>
//					{
//						return Helpers.CreateClass("public abstract", @class_g, "OmniObject", onClassCreated: () =>
//						{
//							StringBuilder methodBuilder = new StringBuilder();
//							IEnumerable<string> parameters = classDeclarationSyntax.GetAttributeParameters("Omni");
//							if (parameters != null)
//							{
//								byte remoteId = 1;
//								foreach (string fullParameter in parameters)
//								{
//									string[] splittedParameters = fullParameter.Split('(', ')');
//									string parameter = splittedParameters[0];
//									methodBuilder.AppendLine($"protected byte {parameter}Id {"{ get; } = "} {remoteId};");
//									methodBuilder.AppendLine($"[Remote({remoteId})]");
//									methodBuilder.AppendLine($"private void {parameter}(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)");
//									methodBuilder.AppendLine("{");
//									methodBuilder.AppendLine("if(IsServer)");
//									if (splittedParameters.Length > 1)
//									{
//										string splittedParameter = splittedParameters[1];
//										if (int.TryParse(splittedParameter, out int parametersCount))
//										{
//											switch (parametersCount)
//											{
//												case 4:
//													methodBuilder.AppendLine($"{parameter}_Server(IOHandler, fromId, toId, stats);");
//													methodBuilder.AppendLine("else");
//													methodBuilder.AppendLine($"{parameter}_Client(IOHandler, fromId, toId, stats);");
//													break;
//												case 3:
//													methodBuilder.AppendLine($"{parameter}_Server(IOHandler, fromId, toId);");
//													methodBuilder.AppendLine("else");
//													methodBuilder.AppendLine($"{parameter}_Client(IOHandler, fromId, toId);");
//													break;
//												case 2:
//													methodBuilder.AppendLine($"{parameter}_Server(IOHandler, fromId);");
//													methodBuilder.AppendLine("else");
//													methodBuilder.AppendLine($"{parameter}_Client(IOHandler, fromId);");
//													break;
//												case 1:
//													methodBuilder.AppendLine($"{parameter}_Server(IOHandler);");
//													methodBuilder.AppendLine("else");
//													methodBuilder.AppendLine($"{parameter}_Client(IOHandler);");
//													break;
//												case 0:
//													methodBuilder.AppendLine($"{parameter}_Server();");
//													methodBuilder.AppendLine("else");
//													methodBuilder.AppendLine($"{parameter}_Client();");
//													break;
//											}
//										}
//										else
//										{
//											if (splittedParameter == "Simple")
//											{
//												methodBuilder.AppendLine($"{parameter}(IOHandler, IsServer, fromId, toId, stats);");
//												methodBuilder.AppendLine("else");
//												methodBuilder.AppendLine($"{parameter}(IOHandler, IsServer, fromId, toId, stats);");
//											}
//										}
//									}
//									else
//									{
//										methodBuilder.AppendLine($"{parameter}_Server(IOHandler, fromId);");
//										methodBuilder.AppendLine("else");
//										methodBuilder.AppendLine($"{parameter}_Client(IOHandler, fromId);");
//									}
//									methodBuilder.AppendLine("}");

//									if (splittedParameters.Length > 1)
//									{
//										string splittedParameter = splittedParameters[1];
//										if (int.TryParse(splittedParameter, out int parametersCount))
//										{
//											switch (parametersCount)
//											{
//												case 4:
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Server(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);");
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Client(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);");
//													break;
//												case 3:
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Server(DataIOHandler IOHandler, ushort fromId, ushort toId);");
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Client(DataIOHandler IOHandler, ushort fromId, ushort toId);");
//													break;
//												case 2:
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Server(DataIOHandler IOHandler, ushort fromId);");
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Client(DataIOHandler IOHandler, ushort fromId);");
//													break;
//												case 1:
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Server(DataIOHandler IOHandler);");
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Client(DataIOHandler IOHandler);");
//													break;
//												case 0:
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Server();");
//													methodBuilder.AppendLine($"protected abstract void {parameter}_Client();");
//													break;
//											}
//										}
//										else
//										{
//											if (splittedParameter == "Simple")
//											{
//												methodBuilder.AppendLine($"protected abstract void {parameter}(DataIOHandler IOHandler, bool isServer, ushort fromId, ushort toId, RemoteStats stats);");
//											}
//										}
//									}
//									else
//									{
//										methodBuilder.AppendLine($"protected abstract void {parameter}_Server(DataIOHandler IOHandler, ushort fromId);");
//										methodBuilder.AppendLine($"protected abstract void {parameter}_Client(DataIOHandler IOHandler, ushort fromId);");
//									}
//									remoteId++;
//								}
//							}
//							return methodBuilder.ToString();
//						});
//					}));

//					builder.AppendLine(Helpers.CreateNamespace(@namespace, () =>
//					{
//						return Helpers.CreateClass("public partial", @class, @class_g, () => $"// Generated code by Omni!");
//					}));
//					context.AddSource(@class_g, builder.ToString());
//				}
//			}
//		}

//		public void Initialize(GeneratorInitializationContext context)
//		{
//			context.RegisterForSyntaxNotifications(() => new OmniSyntaxReceiver());
//		}
//	}

//	internal class OmniSyntaxReceiver : ISyntaxReceiver
//	{
//		public List<ClassDeclarationSyntax> ClassDeclarationSyntaxes { get; } = new List<ClassDeclarationSyntax>();
//		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
//		{
//			if (syntaxNode.GetDeclarationSyntax(out ClassDeclarationSyntax classDeclarationSyntax))
//			{
//				if (classDeclarationSyntax.HasModifier(SyntaxKind.PartialKeyword))
//				{
//					if (classDeclarationSyntax.HasAttribute("Omni"))
//					{
//						//ClassDeclarationSyntaxes.Add(classDeclarationSyntax);
//					}
//				}
//			}
//		}
//	}
//}
