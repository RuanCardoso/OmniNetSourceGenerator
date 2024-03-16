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

									int idIndex = 0;
									var attributeSyntaxes = originalClassDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.ArgumentList != null && y.ArgumentList.Arguments.Count >= 1 && y.Name.ToString() == "Remote"));
									if (attributeSyntaxes.Any())
									{
										foreach (var attributeSyntax in attributeSyntaxes)
										{
											bool selfValue = false;

											var arguments = attributeSyntax.ArgumentList.Arguments;
											var nameExpression = Helper.GetArgumentExpression<LiteralExpressionSyntax>("Name", 0, arguments);
											var idExpression = Helper.GetArgumentExpression<LiteralExpressionSyntax>("Id", 1, arguments);
											var selfExpression = Helper.GetArgumentExpression<LiteralExpressionSyntax>("Self", 2, arguments);

											if (nameExpression == null)
											{
												context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA005", "Omni", "The 'Remote' attribute requires the 'name' argument.", "", DiagnosticSeverity.Error, true), Location.None));
												return;
											}

											idIndex++;
											string idValue = idExpression != null ? idExpression.Token.ValueText : idIndex.ToString();
											string methodName = nameExpression.Token.ValueText;

											if (selfExpression != null)
											{
												if (bool.TryParse(selfExpression.Token.ValueText, out bool selfValueParsed))
												{
													selfValue = selfValueParsed;
												}
											}

											// Id Property
											PropertyDeclarationSyntax idPropertySyntax = SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword)), $"{methodName}Id").WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword)))
											.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseName($"{idValue};")));

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

											//RPC Client Send Method with overloads!
											MethodDeclarationSyntax rpcClientSendMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ClientRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ClientRpc(writer, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

											// Overload 1
											MethodDeclarationSyntax rpcClientSendMethodDeclarationSyntax2 = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ClientRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode = TargetMode.Broadcast")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ClientRpc(writer, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

											// Overload 2
											MethodDeclarationSyntax rpcClientSendMethodDeclarationSyntax3 = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ClientRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode = TargetMode.Broadcast")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ClientRpc(DataWriter.Empty, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

											//RPC Server Send Method with overloads!
											MethodDeclarationSyntax rpcServerSendMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ServerRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("int peerId")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ServerRpc(writer, deliveryMode, peerId, {methodName}Id, sequenceChannel);")));

											MethodDeclarationSyntax rpcServerSendWithBroadcastMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ServerRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ServerRpc(writer, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

											MethodDeclarationSyntax rpcServerSendWithBroadcastMethodDeclarationSyntax2 = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ServerRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataWriter writer")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode = TargetMode.Broadcast")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ServerRpc(writer, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

											MethodDeclarationSyntax rpcServerSendWithBroadcastMethodDeclarationSyntax3 = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), $"{methodName}ServerRpc")
												.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
												.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[]
												 {
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("TargetMode targetMode = TargetMode.Broadcast")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered")),
													   SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte sequenceChannel = 0")),
												 }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"ServerRpc(DataWriter.Empty, deliveryMode, targetMode, {methodName}Id, sequenceChannel);")));

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
											newClassDeclarationSyntax = newClassDeclarationSyntax.AddMembers(idPropertySyntax, rpcMethodDeclarationSyntax.WithLeadingTrivia(SyntaxFactory.Comment("\n")), rpcClientSendMethodDeclarationSyntax, rpcClientSendMethodDeclarationSyntax2, rpcClientSendMethodDeclarationSyntax3, rpcServerSendMethodDeclarationSyntax, rpcServerSendWithBroadcastMethodDeclarationSyntax, rpcServerSendWithBroadcastMethodDeclarationSyntax2, rpcServerSendWithBroadcastMethodDeclarationSyntax3);
										}

										newNamespaceDeclarationSyntax = newNamespaceDeclarationSyntax.AddMembers(newClassDeclarationSyntax);
										if (originalNamespaceDeclarationSyntax == null) sb.Append(newClassDeclarationSyntax.NormalizeWhitespace().ToString());
										else sb.Append(newNamespaceDeclarationSyntax.NormalizeWhitespace().ToString());
									}
									else
									{
										context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CA005", "Omni", "The 'Remote' attribute requires two arguments or more.", "", DiagnosticSeverity.Error, true), Location.None));
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
