using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Generate property to a field with NetworkVariableSync...

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class NetVarGeneratorGenProperty : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is NetworkVariableFieldSyntaxReceiver receiver)
				{
					if (receiver.fields.Any())
					{
						var classes = receiver.fields.GroupByDeclaringClass();
						foreach (ClassStructure @class in classes)
						{
							StringBuilder sb = new StringBuilder();
							sb.AppendLine("#nullable disable");
							sb.AppendLine("#pragma warning disable");
							sb.AppendLine("using Omni.Inspector;");
							sb.AppendLine();

							ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
							foreach (var usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
								sb.AppendLine(usingSyntax.ToString());

							if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
							{
								var classModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
								bool isNetworkBehaviour = fromClass.InheritsFromClass(classModel, "NetworkBehaviour");
								bool isClientBehaviour = fromClass.InheritsFromClass(classModel, "ClientBehaviour");
								bool isServerBehaviour = fromClass.InheritsFromClass(classModel, "ServerBehaviour");

								// Note: DualBehaviour is not supported.
								bool isDualBehaviour = fromClass.InheritsFromClass(classModel, "DualBehaviour");
								if (GenHelper.ReportUnsupportedDualBehaviourUsage(context, isDualBehaviour))
									continue;

								if (isNetworkBehaviour || isClientBehaviour || isServerBehaviour)
								{
									string baseClassName = isNetworkBehaviour ? "NetworkBehaviour" : (isClientBehaviour ? "ClientBehaviour" : isServerBehaviour ? "ServerBehaviour" : null);
									NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
									if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

									List<MemberDeclarationSyntax> memberList = new List<MemberDeclarationSyntax>();
									HashSet<byte> ids = new HashSet<byte>();

									PropertyDeclarationSyntax staticDefaultSettings = SyntaxFactory
										.PropertyDeclaration(SyntaxFactory.ParseTypeName("NetworkVariableOptions"), $"DefaultNetworkVariableOptions")
										.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
										.WithAccessorList(
											SyntaxFactory.AccessorList(
												SyntaxFactory.List(
													new AccessorDeclarationSyntax[]
													{
														SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
														SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
													}
												)
											)
										)
										.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("new NetworkVariableOptions()")))
										.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

									foreach (FieldDeclarationSyntax field in @class.Members.Cast<FieldDeclarationSyntax>())
									{
										byte currentId = 0;
										var attribute = field.GetAttribute("NetworkVariable");
										if (attribute != null)
										{
											var idExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("id", ArgumentIndex.First);
											if (idExpression != null)
											{
												if (byte.TryParse(idExpression.Token.ValueText, out byte idValue))
												{
													currentId = idValue;
												}
											}
										}

										if (currentId <= 0)
										{
											int baseDepth = fromClass.GetBaseDepth(classModel, "NetworkBehaviour", "ClientBehaviour", "ServerBehaviour");
											if (baseDepth != 0)
											{
												unchecked
												{
													currentId = (byte)(255 - (255 / baseDepth));
												}
											}
										}

										if (currentId <= 0)
										{
											currentId++;
											while (ids.Contains(currentId))
											{
												currentId++;
											}
										}

										TypeSyntax declarationType = field.Declaration.Type;
										foreach (VariableDeclaratorSyntax variableSyntax in field.Declaration.Variables)
										{
											string variableName = variableSyntax.Identifier.Text;
											if (GenHelper.ReportInvalidFieldNamingStartsWith(new Context(context), variableName))
											{
												continue;
											}

											// remove m_ prefix
											variableName = variableName.Substring(2);
											if (GenHelper.ReportInvalidFieldNamingIsUpper(new Context(context), variableName))
											{
												continue;
											}

											while (ids.Contains(currentId))
											{
												currentId++;
											}

											ids.Add(currentId);
											memberList.Add(
												SyntaxFactory
													.PropertyDeclaration(SyntaxFactory.ParseTypeName("NetworkVariableOptions"), $"{variableName}Options")
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
													.WithAccessorList(
														SyntaxFactory.AccessorList(
															SyntaxFactory.List(
																new AccessorDeclarationSyntax[]
																{
																	SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
																	SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
																}
															)
														)
													)
													.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("null")))
													.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
											);

											memberList.Add(
												SyntaxFactory.PropertyDeclaration(declarationType, variableName)
													.WithAttributeLists(
														SyntaxFactory.List(
															new AttributeListSyntax[]
															{
																SyntaxFactory.AttributeList(
																	SyntaxFactory.SeparatedList(
																		new AttributeSyntax[]
																		{
																			SyntaxFactory.Attribute(SyntaxFactory.ParseName($"NetworkVariable"),
																			SyntaxFactory.AttributeArgumentList(
																					SyntaxFactory.SeparatedList(
																						new AttributeArgumentSyntax[]
																						{
																							SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"{currentId}"))
																						}
																					)
																				)
																			),
																			SyntaxFactory.Attribute(SyntaxFactory.ParseName($"SerializeProperty")),
																			SyntaxFactory.Attribute(SyntaxFactory.ParseName($"HidePicker")),
																			SyntaxFactory.Attribute(SyntaxFactory.ParseName($"Group"),
																			SyntaxFactory.AttributeArgumentList(
																					SyntaxFactory.SeparatedList(
																						new AttributeArgumentSyntax[]
																						{
																							SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"\"Network Variables\""))
																						}
																					)
																				)
																			),
																		}
																	)
																)
															}
														)
													)
													.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
													.WithAccessorList(
														SyntaxFactory.AccessorList(
															SyntaxFactory.List(
																new AccessorDeclarationSyntax[]
																{
																	SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return m_{variableName};"))),
																	SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithBody(
																		SyntaxFactory.Block(
																			SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression($"!UnityEngine.Application.isPlaying"), SyntaxFactory.Block(SyntaxFactory.ParseStatement($"m_{variableName} = value;"), SyntaxFactory.ParseStatement("return;"))),
																			SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression($"OnNetworkVariableDeepEquals(m_{variableName}, value, \"{variableName}\", {currentId})"), SyntaxFactory.Block(SyntaxFactory.ParseStatement("return;"))),
																			SyntaxFactory.ParseStatement($"On{variableName}Changed(m_{variableName}, value, true);"),
																			SyntaxFactory.ParseStatement($"OnBase{variableName}Changed(m_{variableName}, value, true);"),
																			SyntaxFactory.ParseStatement($"m_{variableName} = value;"),
																			(baseClassName == "NetworkBehaviour")
																				? SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression("IsClient"),
																						SyntaxFactory.Block(
																							SyntaxFactory.ParseStatement($"Client.NetworkVariableSync({variableName}, {currentId}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																						)
																					).WithElse(
																						SyntaxFactory.ElseClause(
																							SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Server.NetworkVariableSync({variableName}, {currentId}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);"))
																						)
																					)
																				: baseClassName == "ServerBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSync({variableName}, {currentId}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																				: baseClassName == "ClientBehaviour" ? SyntaxFactory.ParseStatement($"Client.NetworkVariableSync({variableName}, {currentId}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																				: null
																		)
																	)
																}
															)
														)
													)
											);

											currentId++;
										}
									}

									memberList.Add(staticDefaultSettings);
									parentClass = parentClass.AddMembers(memberList.ToArray()); ;

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

									context.AddSource($"{parentClass.Identifier.Text}_netvar_create_property_generated_code_.cs", sb.ToString());
								}
								else
								{
									GenHelper.ReportInheritanceRequirement(new Context(context), fromClass.Identifier.Text);
								}
							}
							else
							{
								GenHelper.ReportPartialKeywordRequirement(new Context(context), fromClass);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						new DiagnosticDescriptor(
							"OMNI000",
							"Unhandled Exception Detected",
							$"An unhandled exception occurred: {ex.Message}. Stack Trace: {ex.StackTrace}",
							"Runtime",
							DiagnosticSeverity.Error,
							isEnabledByDefault: true
						),
						Location.None
					)
				);
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new NetworkVariableFieldSyntaxReceiver());
		}
	}

	internal class NetworkVariableFieldSyntaxReceiver : ISyntaxReceiver
	{
		internal List<FieldDeclarationSyntax> fields = new List<FieldDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is FieldDeclarationSyntax fieldSyntax)
			{
				if (fieldSyntax.HasAttribute("NetworkVariable"))
				{
					fields.Add(fieldSyntax);
				}
			}
		}
	}
}
