using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Generate property to a field with ManualSync...

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class NetVarGeneratorGenProperty : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is NetworkVariableFieldSyntaxReceiver syntaxReceiver)
				{
					if (syntaxReceiver.fields.Any())
					{
						IEnumerable<ClassStructure> classes = syntaxReceiver.fields.GroupMembersByParentClass();
						foreach (ClassStructure @class in classes)
						{
							StringBuilder sb = new StringBuilder();
							sb.AppendLine("#nullable disable");
							sb.AppendLine("#pragma warning disable");
							sb.AppendLine("using TriInspector;");
							sb.AppendLine();

							ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
							foreach (UsingDirectiveSyntax usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
								sb.AppendLine(usingSyntax.ToString());

							if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
							{
								var semanticModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
								bool isNetworkBehaviour = fromClass.InheritsFrom(semanticModel, "NetworkBehaviour");
								bool isClientBehaviour = fromClass.InheritsFrom(semanticModel, "ClientBehaviour");
								bool isServerBehaviour = fromClass.InheritsFrom(semanticModel, "ServerBehaviour");

								// Note: DualBehaviour is not supported.
								bool isDualBehaviour = fromClass.InheritsFrom(semanticModel, "DualBehaviour");
								if (GenHelper.ReportUnsupportedDualBehaviourUsage(context, isDualBehaviour))
									continue;

								if (isNetworkBehaviour || (isClientBehaviour || isServerBehaviour))
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
										byte id = 0;
										TypeSyntax declarationType = field.Declaration.Type;
										IEnumerable<AttributeSyntax> attributes = field.GetAttributes("NetworkVariable");
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

										if (id <= 0)
										{
											int baseDepth = fromClass.GetBaseDepth(semanticModel, "NetworkBehaviour", "ClientBehaviour", "ServerBehaviour");
											if (baseDepth != 0)
											{
												unchecked
												{
													id = (byte)(255 - (255 / baseDepth));
												}
											}
										}

										if (id <= 0)
										{
											id++;
											while (ids.Contains(id))
											{
												id++;
											}
										}

										foreach (VariableDeclaratorSyntax variableSyntax in field.Declaration.Variables)
										{
											string variableName = variableSyntax.Identifier.Text;
											if (GenHelper.ReportInvalidFieldNaming(context, variableName))
											{
												continue;
											}

											// remove m_ prefix
											variableName = variableName.Substring(2);
											if (GenHelper.ReportInvalidFieldNamingConvention(context, variableName))
											{
												continue;
											}

											while (ids.Contains(id))
											{
												id++;
											}

											ids.Add(id);
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
																							SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"{id}"))
																						}
																					)
																				)
																			),
																			SyntaxFactory.Attribute(SyntaxFactory.ParseName($"SerializeProperty")),
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
																			SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression($"DeepEquals(m_{variableName}, value, \"{variableName}\")"), SyntaxFactory.Block(SyntaxFactory.ParseStatement("return;"))),
																			SyntaxFactory.ParseStatement($"On{variableName}Changed(m_{variableName}, value, true);"),
																			SyntaxFactory.ParseStatement($"OnBase{variableName}Changed(m_{variableName}, value, true);"),
																			SyntaxFactory.ParseStatement($"m_{variableName} = value;"),
																			(baseClassName == "NetworkBehaviour")
																				? SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression("IsMine"),
																						SyntaxFactory.Block(
																							SyntaxFactory.ParseStatement($"Local.ManualSync({variableName}, {id}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																						)
																					).WithElse(
																						SyntaxFactory.ElseClause(SyntaxFactory.Block(
																								SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression("IsServer"),
																								SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Remote.ManualSync({variableName}, {id}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")),
																								SyntaxFactory.ElseClause(SyntaxFactory.Block(
																									SyntaxFactory.ParseStatement("throw new System.InvalidOperationException(\"You are trying to modify a variable that you have no authority over, be sure to check IsMine/IsLocalPlayer/IsServer/IsClient.\");")))
																								)
																							)
																						)
																					)
																				: baseClassName == "ServerBehaviour" ? SyntaxFactory.ParseStatement($"Remote.ManualSync({variableName}, {id}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																				: baseClassName == "ClientBehaviour" ? SyntaxFactory.ParseStatement($"Local.ManualSync({variableName}, {id}, {variableName}Options != null ? {variableName}Options : DefaultNetworkVariableOptions);")
																				: null
																		)
																	)
																}
															)
														)
													)
											);

											id++;
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

									context.AddSource($"{parentClass.Identifier.ToFullString()}_netvar_create_property_generated_code.cs", sb.ToString());
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
