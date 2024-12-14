using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

// Generate serializer and deserialize methods.
// override ___OnPropertyChanged___ and etc.

namespace OmniNetSourceGenerator
{
	[Generator]
	internal class NetVarGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is NetworkVariableSyntaxReceiver syntaxReceiver)
				{
					if (syntaxReceiver.members.Any())
					{
						IEnumerable<ClassStructure> classes = syntaxReceiver.members.GroupMembersByParentClass();
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
								bool isClientBehaviour = fromClass.InheritsFrom(semanticModel, "ClientBehaviour");
								bool isServerBehaviour = fromClass.InheritsFrom(semanticModel, "ServerBehaviour");

								// Note: DualBehaviour is not supported.
								bool isDualBehaviour = fromClass.InheritsFrom(semanticModel, "DualBehaviour");
								if (GenHelper.ReportUnsupportedDualBehaviourUsage(context, isDualBehaviour))
									continue;

								if (isNetworkBehaviour || (isClientBehaviour || isServerBehaviour))
								{
									string isServer = DetermineIsServerValue(isNetworkBehaviour, isClientBehaviour, isServerBehaviour, out string baseClassName);
									NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
									if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

									List<SwitchSectionSyntax> sections = new List<SwitchSectionSyntax>();
									List<MethodDeclarationSyntax> onChangedHandlers = new List<MethodDeclarationSyntax>();

									List<StatementSyntax> onNotifyCollectionHandlers = new List<StatementSyntax>();

									HashSet<byte> ids = new HashSet<byte>();
									foreach (MemberDeclarationSyntax member in @class.Members)
									{
										byte id = 0;

										TypeSyntax declarationType = member is FieldDeclarationSyntax field ? field.Declaration.Type : ((PropertyDeclarationSyntax)member).Type;
										SemanticModel model = context.Compilation.GetSemanticModel(member.SyntaxTree);

										bool isSerializable = IsSerializable(model.GetTypeInfo(declarationType).Type, out bool withPeer);
										IEnumerable<AttributeSyntax> attributes = member.GetAttributes("NetworkVariable");
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

										var typeString = declarationType.ToString();
										if (member is PropertyDeclarationSyntax propSyntax)
										{
											string identifierName = propSyntax.Identifier.Text;
											while (ids.Contains(id))
											{
												id++;
											}

											ids.Add(id);
											sections.Add(CreateSection(id.ToString(), identifierName, typeString, isSerializable, withPeer, isServer));

											onChangedHandlers.Add(CreatePartialHandler(identifierName, typeString));
											onChangedHandlers.Add(CreateVirtualHandler(identifierName, typeString));
											onChangedHandlers.Add(CreateSyncMethod(identifierName, id.ToString(), baseClassName));

											if (typeString.StartsWith("ObservableDictionary") || typeString.StartsWith("ObservableList"))
											{
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemAdded += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemRemoved += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemUpdated += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnUpdate += (isSend) => {{if(isSend) Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);}};"));
											}
										}
										else if (member is FieldDeclarationSyntax fieldSyntax)
										{
											foreach (var variable in fieldSyntax.Declaration.Variables)
											{
												// Check if the field name follows the naming convention
												string fieldName = variable.Identifier.Text;
												if (GenHelper.ReportInvalidFieldNaming(context, fieldName))
												{
													continue;
												}

												// Remove m_ prefix
												string variableName = fieldName.Substring(2);
												if (GenHelper.ReportInvalidFieldNamingConvention(context, variableName))
												{
													continue;
												}

												while (ids.Contains(id))
												{
													id++;
												}

												ids.Add(id);
												sections.Add(CreateSection(id.ToString(), variableName, typeString, isSerializable, withPeer, isServer));

												onChangedHandlers.Add(CreatePartialHandler(variableName, typeString));
												onChangedHandlers.Add(CreateVirtualHandler(variableName, typeString));
												onChangedHandlers.Add(CreateSyncMethod(variableName, id.ToString(), baseClassName));

												if (typeString.StartsWith("ObservableDictionary") || typeString.StartsWith("ObservableList"))
												{
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemAdded += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemRemoved += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemUpdated += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnUpdate += (isSend) => {{if(isSend) Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);}};"));
												}
											}
										}
									}

									MethodDeclarationSyntax onServerPropertyChanged = CreatePropertyMethod($"___OnServerPropertyChanged___{parentClass.Identifier.Text}___", "Server")
										.WithParameterList(
											SyntaxFactory.ParameterList(
												SyntaxFactory.SeparatedList(
													new ParameterSyntax[]
													{
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataBuffer buffer")),
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer"))
													}
												)
											)
										).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("___OnPropertyChanged___(default, default, peer, buffer);")));

									MethodDeclarationSyntax onClientPropertyChanged = CreatePropertyMethod($"___OnClientPropertyChanged___{parentClass.Identifier.Text}___", "Client")
										.WithParameterList(
											SyntaxFactory.ParameterList(
												SyntaxFactory.SeparatedList(
													new ParameterSyntax[]
													{
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataBuffer buffer")),
													}
												)
											)
										).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("___OnPropertyChanged___(default, default, default, buffer);")));

									MethodDeclarationSyntax onPropertyChanged = SyntaxFactory
										.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "___OnPropertyChanged___")
										.WithModifiers(
											SyntaxFactory.TokenList(
												SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
												SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
											)
										)
										.WithParameterList(
											SyntaxFactory.ParameterList(
												SyntaxFactory.SeparatedList(
													new ParameterSyntax[]
													{
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("string propertyName")),
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte propertyId")),
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer")),
														SyntaxFactory.Parameter(SyntaxFactory.Identifier("DataBuffer buffer"))
													}
												)
											)
										).WithBody(
										SyntaxFactory.Block(SyntaxFactory.ParseStatement("propertyId = buffer.Read<byte>();"),
										SyntaxFactory.SwitchStatement(SyntaxFactory.ParseExpression("propertyId")).WithSections(SyntaxFactory.List(sections)),
										SyntaxFactory.ParseStatement("buffer.SeekToBegin();"),
										SyntaxFactory.ParseStatement("base.___OnPropertyChanged___(propertyName, propertyId, peer, buffer);")));

									onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"base.___NotifyCollectionChange___();"));

									MethodDeclarationSyntax onNotifyCollectionChange = SyntaxFactory
										.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "___NotifyCollectionChange___")
										.WithModifiers(
											SyntaxFactory.TokenList(
												SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
												SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
											)
										).WithBody(SyntaxFactory.Block(SyntaxFactory.List(onNotifyCollectionHandlers)));

									parentClass = parentClass.AddMembers(
										onServerPropertyChanged,
										onClientPropertyChanged,
										onPropertyChanged,
										onNotifyCollectionChange
									);

									parentClass = parentClass.AddMembers(onChangedHandlers.ToArray());

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

									context.AddSource($"{parentClass.Identifier.ToFullString()}_netvar_generated_code.cs", sb.ToString());
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

		private string DetermineIsServerValue(bool isNetworkBehaviour, bool isClientBehaviour, bool isServerBehaviour, out string baseClassName)
		{
			if (isNetworkBehaviour)
			{
				baseClassName = "NetworkBehaviour";
				return "IsServer";
			}
			else
			{
				if (isClientBehaviour)
				{
					baseClassName = "ClientBehaviour";
					return "false";
				}
				else if (isServerBehaviour)
				{
					baseClassName = "ServerBehaviour";
					return "true";
				}
			}

			throw new NotImplementedException("Unable to determine 'isServer' value");
		}

		private bool IsSerializable(ITypeSymbol typeSymbol, out bool withPeer)
		{
			withPeer = false;
			if (typeSymbol != null)
			{
				var interfaces = typeSymbol.Interfaces;
				withPeer = interfaces.Any(x => x.Name == "IMessageWithPeer");
				return interfaces.Any(x => x.Name == "IMessageWithPeer" || x.Name == "IMessage");
			}

			return false;
		}

		private MethodDeclarationSyntax CreateSyncMethod(string propertyName, string propertyId, string baseClassName)
		{
			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"Sync{propertyName}")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
				.WithParameterList(
					SyntaxFactory.ParameterList(
						SyntaxFactory.SeparatedList(
							new ParameterSyntax[]
							{
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"options")).WithType(SyntaxFactory.ParseTypeName("NetworkVariableOptions"))
							}
						)
					)
				)
				.WithBody(
					SyntaxFactory.Block(
						SyntaxFactory.ParseStatement("options ??= new();"),
						baseClassName == "NetworkBehaviour"
							? SyntaxFactory
								.IfStatement(
									SyntaxFactory.ParseExpression("IsClient"),
									SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Client.NetworkVariableSync({propertyName}, {propertyId}, options);"))
								)
								.WithElse(
									SyntaxFactory.ElseClause(
										SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Server.NetworkVariableSync({propertyName}, {propertyId}, options);"))
									)
								)
							: baseClassName == "ServerBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSync({propertyName}, {propertyId}, options);")
							: baseClassName == "ClientBehaviour" ? SyntaxFactory.ParseStatement($"Client.NetworkVariableSync({propertyName}, {propertyId}, options);")
							: null
					)
				);
		}

		private MethodDeclarationSyntax CreatePartialHandler(string propertyName, string type)
		{
			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"On{propertyName}Changed")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
				.WithParameterList(
					SyntaxFactory.ParameterList(
						SyntaxFactory.SeparatedList(
							new ParameterSyntax[]
							{
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"prev{propertyName}")).WithType(SyntaxFactory.ParseTypeName(type)),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"next{propertyName}")).WithType(SyntaxFactory.ParseTypeName(type)),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier("isWriting")).WithType(SyntaxFactory.ParseTypeName("bool"))
							}
						)
					)
				).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
		}

		private MethodDeclarationSyntax CreateVirtualHandler(string propertyName, string type)
		{
			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"OnBase{propertyName}Changed")
				.WithModifiers(
					SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
				)
				.WithParameterList(
					SyntaxFactory.ParameterList(
						SyntaxFactory.SeparatedList(
							new ParameterSyntax[]
							{
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"prev{propertyName}")).WithType(SyntaxFactory.ParseTypeName(type)),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"next{propertyName}")).WithType(SyntaxFactory.ParseTypeName(type)),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier("isWriting")).WithType(SyntaxFactory.ParseTypeName("bool"))
							}
						)
					)
				).WithBody(SyntaxFactory.Block());
		}

		private SwitchSectionSyntax CreateSection(
			string caseExpression, // propertyId
			string propertyName,
			string propertyType,
			bool isSerializable,
			bool isSerializableWithPeer,
			string isServer
		)
		{
			return !isSerializable
				? SyntaxFactory.SwitchSection(
					SyntaxFactory.List(
						new SwitchLabelSyntax[]
						{
							SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(caseExpression))
						}
					),
					SyntaxFactory.List(
						new StatementSyntax[]
						{
							SyntaxFactory.Block(
								SyntaxFactory.ParseStatement($"propertyName = \"{propertyName}\";"),
								SyntaxFactory.ParseStatement($"var nextValue = buffer.ReadAsBinary<{propertyType}>();"),
								SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression($"!OnNetworkVariableDeepEquals(m_{propertyName}, nextValue, propertyName, {caseExpression})"),
								SyntaxFactory.Block(SyntaxFactory.ParseStatement($"On{propertyName}Changed(m_{propertyName}, nextValue, false);"),
								SyntaxFactory.ParseStatement($"OnBase{propertyName}Changed(m_{propertyName}, nextValue, false);"),
								(propertyType.StartsWith("ObservableDictionary") || propertyType.StartsWith("ObservableList")) ? SyntaxFactory.ParseStatement("nextValue.OnUpdate?.Invoke(false);") : SyntaxFactory.EmptyStatement(),
								SyntaxFactory.ParseStatement($"m_{propertyName} = nextValue;")),
								SyntaxFactory.ElseClause(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return;")))),
								SyntaxFactory.BreakStatement()
							),
						}
					)
				) // Start of deserialize
				: SyntaxFactory.SwitchSection(
					SyntaxFactory.List(
						new SwitchLabelSyntax[]
						{
							SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(caseExpression))
						}
					),
					SyntaxFactory.List(
						new StatementSyntax[]
						{
							SyntaxFactory.Block(
								SyntaxFactory.ParseStatement($"propertyName = \"{propertyName}\";"),
								SyntaxFactory.ParseStatement("using var nBuffer = NetworkManager.Pool.Rent();"),
								SyntaxFactory.ParseStatement("nBuffer.RawWrite(buffer.GetSpan().Slice(0, buffer.Length)); // from current position to end > skip propertyId(header)"),
								SyntaxFactory.ParseStatement($"nBuffer.SeekToBegin();"),
								isSerializableWithPeer
									? SyntaxFactory.ParseStatement($"var nextValue = nBuffer.Deserialize<{propertyType}>(peer != null ? peer : NetworkManager.LocalPeer, {isServer});")
									: SyntaxFactory.ParseStatement($"var nextValue = nBuffer.Deserialize<{propertyType}>();"),
								SyntaxFactory.ParseStatement($"On{propertyName}Changed(m_{propertyName}, nextValue, false);"),
								SyntaxFactory.ParseStatement($"OnBase{propertyName}Changed(m_{propertyName}, nextValue, false);"),
								SyntaxFactory.ParseStatement($"m_{propertyName} = nextValue;"),
								SyntaxFactory.BreakStatement()
							),
						}
					)
				);
		}

		private MethodDeclarationSyntax CreatePropertyMethod(string methodName, string attributeName)
		{
			MethodDeclarationSyntax onPropertyChanged = SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), methodName)
				.WithAttributeLists(
					SyntaxFactory.List(
						new AttributeListSyntax[]
						{
							SyntaxFactory.AttributeList(
								SyntaxFactory.SingletonSeparatedList(
									SyntaxFactory
										.Attribute(SyntaxFactory.IdentifierName(attributeName))
										.WithArgumentList(
											SyntaxFactory.AttributeArgumentList(
												SyntaxFactory.SeparatedList(
													new AttributeArgumentSyntax[]
													{
														SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("255"))
													}
												)
											)
										)
								)
							)
						}
					)
				)
				.WithModifiers(
					SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
				);

			return onPropertyChanged;
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new NetworkVariableSyntaxReceiver());
		}
	}

	internal class NetworkVariableSyntaxReceiver : ISyntaxReceiver
	{
		internal List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is MemberDeclarationSyntax memberSyntax)
			{
				if (memberSyntax.HasAttribute("NetworkVariable"))
				{
					members.Add(memberSyntax);
				}
			}
		}
	}
}
