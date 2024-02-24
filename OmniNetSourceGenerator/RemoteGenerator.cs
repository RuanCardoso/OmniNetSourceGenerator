using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class RemoteGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is RemoteSyntaxReceiver remoteSyntaxReceiver)
				{
					if (remoteSyntaxReceiver.classDeclarationSyntaxes.Any())
					{
						foreach (var originalClassDeclarationSyntax in remoteSyntaxReceiver.classDeclarationSyntaxes)
						{
							StringBuilder sb = new StringBuilder();
							#region Usings
							IEnumerable<UsingDirectiveSyntax> usingDirectiveSyntaxes = originalClassDeclarationSyntax.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>();
							foreach (UsingDirectiveSyntax usingDirectiveSyntax in usingDirectiveSyntaxes)
								sb.AppendLine(usingDirectiveSyntax.ToString());
							#endregion

							SemanticModel semanticModel = context.Compilation.GetSemanticModel(originalClassDeclarationSyntax.SyntaxTree);
							if (originalClassDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
							{
								if (originalClassDeclarationSyntax.BaseList != null && originalClassDeclarationSyntax.BaseList.Types.Any(x => x.ToString() == "NetworkBehaviour"))
								{
									NamespaceDeclarationSyntax originalNamespaceDeclarationSyntax = originalClassDeclarationSyntax.Parent is NamespaceDeclarationSyntax @namespace ? @namespace : null;
									NamespaceDeclarationSyntax newNamespaceDeclarationSyntax = SyntaxFactory.NamespaceDeclaration(originalNamespaceDeclarationSyntax != null ? originalNamespaceDeclarationSyntax.Name : SyntaxFactory.ParseName("@UNDEFINED"));
									ClassDeclarationSyntax newClassDeclarationSyntax = SyntaxFactory.ClassDeclaration(originalClassDeclarationSyntax.Identifier.Text).
										WithModifiers(originalClassDeclarationSyntax.Modifiers).WithBaseList(originalClassDeclarationSyntax.BaseList);

									var attributeSyntaxes = originalClassDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.ArgumentList != null && y.ArgumentList.Arguments.Count == 3 && y.Name.ToString() == "Remote"));
									if (attributeSyntaxes.Any())
									{
										foreach (var attributeSyntax in attributeSyntaxes)
										{
											var arguments = attributeSyntax.ArgumentList.Arguments;
											var idExpression = arguments.First(x => x.NameEquals.Name.Identifier.Text == "Id");
											var nameExpression = arguments.First(x => x.NameEquals.Name.Identifier.Text == "Name");
											var selfExpression = arguments.First(x => x.NameEquals.Name.Identifier.Text == "Self");

											string idValue = ((LiteralExpressionSyntax)idExpression.Expression).Token.ValueText;
											string methodName = ((LiteralExpressionSyntax)nameExpression.Expression).Token.ValueText;
											if (bool.TryParse(((LiteralExpressionSyntax)selfExpression.Expression).Token.ValueText, out bool selfValue))
											{
												// RPC Method
												MethodDeclarationSyntax rpcMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), !selfValue ? $"__{Math.Abs(methodName.GetHashCode())}" : methodName)
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(selfValue ? SyntaxKind.PartialKeyword : SyntaxKind.PrivateKeyword))).
													WithAttributeLists(SyntaxFactory.List(new AttributeListSyntax[]
													{
													SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new AttributeSyntax[]
													{
														attributeSyntax,
													})),
													}))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataReader reader")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer")),
													 })));

												rpcMethodDeclarationSyntax = !selfValue
													? rpcMethodDeclarationSyntax.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(string.Concat("if (IsServer) {", $"{methodName}_Server(reader, peer);", "}", "else {", $"{methodName}_Client(reader, peer);", "}"))))
													: rpcMethodDeclarationSyntax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

												//RPC Send Method
												MethodDeclarationSyntax rpcClientSendMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), methodName)
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataDeliveryMode dataDeliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
													 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Rpc(writer, dataDeliveryMode, {idValue}, sequenceChannel: sequenceChannel);")));

												MethodDeclarationSyntax rpcServerSendMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), methodName)
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataDeliveryMode dataDeliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("int peerId")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
													 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Rpc(writer, dataDeliveryMode, peerId, {idValue}, sequenceChannel: sequenceChannel);")));

												MethodDeclarationSyntax rpcSendWithBroadcastMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), methodName)
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataDeliveryMode dataDeliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataTarget dataTarget")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
													 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Rpc(writer, dataDeliveryMode, dataTarget, {idValue}, sequenceChannel: sequenceChannel);")));

												// Partial methods
												MethodDeclarationSyntax rpcServerMethod = null;
												MethodDeclarationSyntax rpcClientMethod = null;
												if (!selfValue)
												{
													#region Partial Methods
													rpcServerMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}_Server")
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataReader reader")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer")),
													 }))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

													rpcClientMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}_Client")
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
													.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
													 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataReader reader")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer")),
													 }))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
													#endregion;

													newClassDeclarationSyntax = newClassDeclarationSyntax.AddMembers(rpcServerMethod, rpcClientMethod);
												}
												newClassDeclarationSyntax = newClassDeclarationSyntax.AddMembers(rpcMethodDeclarationSyntax, rpcClientSendMethodDeclarationSyntax, rpcServerSendMethodDeclarationSyntax, rpcSendWithBroadcastMethodDeclarationSyntax);
											}
										}

										newNamespaceDeclarationSyntax = newNamespaceDeclarationSyntax.AddMembers(newClassDeclarationSyntax);
										if (originalNamespaceDeclarationSyntax == null) sb.Append(newClassDeclarationSyntax.NormalizeWhitespace().ToString());
										else sb.Append(newNamespaceDeclarationSyntax.NormalizeWhitespace().ToString());
									}
									else
									{
										context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA005", "Omni", "The 'Remote' attribute requires three arguments.", "", DiagnosticSeverity.Error, true), Location.None));
									}
								}
								else
								{
									context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA004", "Omni", "The class must inherit from NetworkBehaviour.", "", DiagnosticSeverity.Error, true), Location.None));
								}
							}
							else
							{
								context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA003", "Omni", "The class must contain the 'partial' keyword.", "", DiagnosticSeverity.Error, true), Location.None));
							}
							context.AddSource($"{originalClassDeclarationSyntax.Identifier.ToFullString()}_remote_gen_code.cs", sb.ToString());
						}
					}
					else
					{
						// context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA002", "Omni", "No component inheriting from NetworkBehaviour was found.", "", DiagnosticSeverity.Warning, true), Location.None));
					}
				}
			}
			catch (Exception ex)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA001", "Omni", ex.ToString(), "", DiagnosticSeverity.Error, true), Location.None));
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new RemoteSyntaxReceiver());
		}
	}

	internal class RemoteSyntaxReceiver : ISyntaxReceiver
	{
		internal List<ClassDeclarationSyntax> classDeclarationSyntaxes = new List<ClassDeclarationSyntax>();
		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
			{
				if (classDeclarationSyntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Remote")))
				{
					classDeclarationSyntaxes.Add(classDeclarationSyntax);
				}
			}
		}
	}
}
