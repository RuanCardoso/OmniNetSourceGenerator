using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

// Generate serializer and deserialize methods.
// override ___OnPropertyChanged___ and etc.

namespace OmniNetSourceGenerator
{

	public enum Target
	{
		/// <summary>
		/// Automatically selects the most appropriate recipients for the network message based on the current context.
		/// <para>
		/// On the server, this typically means broadcasting to all relevant clients. On the client, it may target the server or a specific group,
		/// depending on the operation being performed. This is the default and recommended option for most use cases.
		/// </para>
		/// </summary>
		Auto,

		/// <summary>
		/// Sends the message to all connected peers, including the sender itself.
		/// <para>
		/// Use this to broadcast updates or events that should be visible to every participant in the session, including the originator.
		/// </para>
		/// </summary>
		Everyone,

		/// <summary>
		/// Sends the message exclusively to the sender (the local peer).
		/// <para>
		/// This option is typically used for providing immediate feedback, confirmations, or updates that are only relevant to the sender and should not be visible to other peers.
		/// </para>
		/// <para>
		/// <b>Note:</b> If the sender is the server (peer id: 0), the message will be ignored and not processed. This ensures that server-only operations do not result in unnecessary or redundant network traffic.
		/// </para>
		/// </summary>
		Self,

		/// <summary>
		/// Sends the message to all connected peers except the sender.
		/// <para>
		/// Use this to broadcast information to all participants while excluding the originator, such as when relaying a player's action to others.
		/// </para>
		/// </summary>
		Others,

		/// <summary>
		/// Sends the message to all peers who are members of the same group(s) as the sender.
		/// <para>
		/// Sub-groups are not included. This is useful for group-based communication, such as team chat or localized events.
		/// </para>
		/// </summary>
		Group,

		/// <summary>
		/// Sends the message to all peers in the same group(s) as the sender, excluding the sender itself.
		/// <para>
		/// Sub-groups are not included. Use this to notify group members of an action performed by the sender, without echoing it back.
		/// </para>
		/// </summary>
		GroupOthers,
	}

	public enum DeliveryMode : byte
	{
		/// <summary>
		/// Ensures packets are delivered reliably and in the exact order they were sent.
		/// No packets will be dropped, duplicated, or arrive out of order.
		/// </summary>
		ReliableOrdered,

		/// <summary>
		/// Sends packets without guarantees. Packets may be dropped, duplicated, or arrive out of order.
		/// This mode offers the lowest latency but no reliability.
		/// </summary>
		Unreliable,

		/// <summary>
		/// Ensures packets are delivered reliably but without enforcing any specific order.
		/// Packets won't be dropped or duplicated, but they may arrive out of sequence.
		/// </summary>
		ReliableUnordered,

		/// <summary>
		/// Sends packets without reliability but guarantees they will arrive in order.
		/// Packets may be dropped, but no duplicates will occur, and order is preserved.
		/// </summary>
		Sequenced,

		/// <summary>
		/// Ensures only the latest packet in a sequence is delivered reliably and in order.
		/// Intermediate packets may be dropped, but duplicates will not occur, and the last packet is guaranteed.
		/// This mode does not support fragmentation.
		/// </summary>
		ReliableSequenced
	}

	[Generator]
	internal class NetVarGenerator : ISourceGenerator
	{
		public static int Hash1To230(string input)
		{
			// Constantes FNV-1a de 32 bits
			const uint FNV_OFFSET_BASIS = 2166136261;
			const uint FNV_PRIME = 16777619;

			// Inicializa o hash com o offset basis
			uint hash = FNV_OFFSET_BASIS;

			// Converte a string para bytes UTF-8
			byte[] bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);

			// Processa cada byte da string
			foreach (byte b in bytes)
			{
				// XOR do hash atual com o byte
				hash ^= b;
				// Multiplicação pelo primo FNV
				hash *= FNV_PRIME;
			}

			// Mapeia o resultado para o intervalo [1, 230]
			int result = (int)(hash % 230) + 1;

			return result;
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!GenHelper.WillProcess(context.Compilation.Assembly))
			{
				return;
			}

			try
			{
				if (context.SyntaxReceiver is NetworkVariableSyntaxReceiver receiver)
				{
					if (receiver.members.Any())
					{
						var classes = receiver.members.GroupByDeclaringClass();
						foreach (ClassStructure @class in classes)
						{
							StringBuilder sb = new StringBuilder();
							sb.AppendLine("#nullable disable");
							sb.AppendLine("#pragma warning disable");
							sb.AppendLine("using System.Buffers;");
							sb.AppendLine();

							ClassDeclarationSyntax parentClass = @class.ParentClass.Clear(out var fromClass);
							foreach (var usingSyntax in fromClass.SyntaxTree.GetRoot().GetDescendantsOfType<UsingDirectiveSyntax>())
								sb.AppendLine(usingSyntax.ToString());

							if (parentClass.HasModifier(SyntaxKind.PartialKeyword))
							{
								var classModel = context.Compilation.GetSemanticModel(fromClass.SyntaxTree);
								var uniqueIds = NetVarGeneratorGenProperty.GetCollectionOfNetworkVariables(classModel.GetDeclaredSymbol(fromClass));

								bool isNetworkBehaviour = fromClass.InheritsFromClass(classModel, "NetworkBehaviour");
								bool isClientBehaviour = fromClass.InheritsFromClass(classModel, "ClientBehaviour");
								bool isServerBehaviour = fromClass.InheritsFromClass(classModel, "ServerBehaviour");
								bool isDualBehaviour = fromClass.InheritsFromClass(classModel, "DualBehaviour");
								if (isNetworkBehaviour || isClientBehaviour || isServerBehaviour || isDualBehaviour)
								{
									string determinedValue = DetermineIsServerValue(isNetworkBehaviour, isClientBehaviour, isServerBehaviour, isDualBehaviour, out string baseClassName);
									NamespaceDeclarationSyntax currentNamespace = fromClass.GetNamespace(out bool hasNamespace);
									if (hasNamespace) currentNamespace = currentNamespace.Clear(out _);

									List<SwitchSectionSyntax> sections = new List<SwitchSectionSyntax>();
									List<MethodDeclarationSyntax> onChangedHandlers = new List<MethodDeclarationSyntax>();
									List<StatementSyntax> networkVariablesRegister = new List<StatementSyntax>();

									List<StatementSyntax> onNotifyCollectionHandlers = new List<StatementSyntax>();
									List<StatementSyntax> onNotifyInitialStateHandlers = new List<StatementSyntax>();

									foreach (MemberDeclarationSyntax member in @class.Members)
									{
										byte currentId = 0;
										bool requiresOwnership = true;
										bool isClientAuthority = false;
										string isServerBroadcastsClientUpdates = "true";
										bool checkEquality = true;
										DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered;
										Target target = Target.Auto;
										byte sequenceChannel = 0;

										TypeSyntax declarationType = member is FieldDeclarationSyntax field ? field.Declaration.Type : ((PropertyDeclarationSyntax)member).Type;
										SemanticModel memberModel = context.Compilation.GetSemanticModel(member.SyntaxTree);
										ITypeSymbol typeSymbol = memberModel.GetTypeInfo(declarationType).Type;

										bool isSerializable = declarationType.InheritsFromInterface(memberModel, "IMessage");
										bool isSerializableWithPeer = declarationType.InheritsFromInterface(memberModel, "IMessageWithPeer");
										bool isDelegate = typeSymbol.IsDelegate();

										var attribute = member.GetAttribute("NetworkVariable");
										if (attribute != null)
										{
											currentId = attribute.GetArgumentValue<byte>("id", ArgumentIndex.First, classModel, 0);
											requiresOwnership = attribute.GetArgumentValue<bool>("RequiresOwnership", ArgumentIndex.Second, classModel, true);
											isClientAuthority = attribute.GetArgumentValue<bool>("IsClientAuthority", ArgumentIndex.Third, classModel, false);
											checkEquality = attribute.GetArgumentValue<bool>("CheckEquality", ArgumentIndex.Fourth, classModel, true);
											deliveryMode = attribute.GetArgumentValue<DeliveryMode>("DeliveryMode", ArgumentIndex.Fifth, classModel, DeliveryMode.ReliableOrdered);
											target = attribute.GetArgumentValue<Target>("Target", ArgumentIndex.Sixth, classModel, Target.Auto);
											sequenceChannel = attribute.GetArgumentValue<byte>("SequenceChannel", ArgumentIndex.Seventh, classModel, 0);
											isServerBroadcastsClientUpdates = attribute.GetArgumentValue<string>("ServerBroadcastsClientUpdates", ArgumentIndex.Eighth, classModel, "true").ToLowerInvariant();
										}

										var typeString = declarationType.ToString();
										if (member is PropertyDeclarationSyntax propSyntax)
										{
											string identifierName = propSyntax.Identifier.Text;
											if (currentId <= 0)
												currentId = (byte)Hash1To230(identifierName);

											if (!isDelegate)
											{
												sections.Add(CreateSection(currentId.ToString(), identifierName, typeString, isSerializable, isSerializableWithPeer, isServerBroadcastsClientUpdates, determinedValue));
											}
											else
											{
												sections.Add(CreateDelegateSection(currentId.ToString(), identifierName, typeString, isSerializable, isSerializableWithPeer, determinedValue, typeSymbol.GetDelegateParameters()));
											}

											if (!isDelegate)
											{
												onChangedHandlers.Add(CreatePartialHandler(identifierName, declarationType));
												onChangedHandlers.Add(CreateVirtualHandler(identifierName, declarationType));
											}

											if (!isDelegate)
											{
												onChangedHandlers.Add(CreateSyncMethod(identifierName, currentId.ToString(), baseClassName));
											}
											else
											{
												onChangedHandlers.Add(CreateInvokeMethod(identifierName, currentId.ToString(), baseClassName, typeSymbol.GetDelegateParameters()));
											}

											if (typeString.StartsWith("ObservableDictionary") || typeString.StartsWith("ObservableList"))
											{
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemAdded += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemRemoved += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnItemUpdated += (_, _) => Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);"));
												onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{identifierName}.OnUpdate += (isSend) => {{if(isSend) Sync{identifierName}({identifierName}Options ?? DefaultNetworkVariableOptions);}};"));
											}

											if (!isDelegate)
												onNotifyInitialStateHandlers.Add(CreateSyncInitialState(identifierName, currentId.ToString(), baseClassName));

											networkVariablesRegister.Add(CreateRegisterNetworkVariable(identifierName, currentId, requiresOwnership, isClientAuthority, checkEquality, $"DeliveryMode.{deliveryMode}", $"Target.{target}", sequenceChannel));
										}
										else if (member is FieldDeclarationSyntax fieldSyntax)
										{
											foreach (var variable in fieldSyntax.Declaration.Variables)
											{
												// Check if the field name follows the naming convention
												string fieldName = variable.Identifier.Text;
												if (GenHelper.ReportInvalidFieldNamingStartsWith(new Context(context), fieldName))
												{
													continue;
												}

												if (currentId <= 0)
													currentId = uniqueIds[fieldName];

												// Remove m_ prefix
												string variableName = fieldName.Substring(2);
												if (GenHelper.ReportInvalidFieldNamingIsUpper(new Context(context), variableName))
												{
													continue;
												}

												if (!isDelegate)
												{
													sections.Add(CreateSection(currentId.ToString(), variableName, typeString, isSerializable, isSerializableWithPeer, isServerBroadcastsClientUpdates, determinedValue));
												}
												else
												{
													sections.Add(CreateDelegateSection(currentId.ToString(), variableName, typeString, isSerializable, isSerializableWithPeer, determinedValue, typeSymbol.GetDelegateParameters()));
												}

												if (!isDelegate)
												{
													onChangedHandlers.Add(CreatePartialHandler(variableName, declarationType));
													onChangedHandlers.Add(CreateVirtualHandler(variableName, declarationType));
												}

												if (!isDelegate)
												{
													onChangedHandlers.Add(CreateSyncMethod(variableName, currentId.ToString(), baseClassName));
												}
												else
												{
													onChangedHandlers.Add(CreateInvokeMethod(variableName, currentId.ToString(), baseClassName, typeSymbol.GetDelegateParameters()));
												}

												if (typeString.StartsWith("ObservableDictionary") || typeString.StartsWith("ObservableList"))
												{
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemAdded += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemRemoved += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnItemUpdated += (_, _) => Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);"));
													onNotifyCollectionHandlers.Add(SyntaxFactory.ParseStatement($"{variableName}.OnUpdate += (isSend) => {{if(isSend) Sync{variableName}({variableName}Options ?? DefaultNetworkVariableOptions);}};"));
												}

												if (!isDelegate)
													onNotifyInitialStateHandlers.Add(CreateSyncInitialState(variableName, currentId.ToString(), baseClassName));

												networkVariablesRegister.Add(CreateRegisterNetworkVariable(variableName, currentId, requiresOwnership, isClientAuthority, checkEquality, $"DeliveryMode.{deliveryMode}", $"Target.{target}", sequenceChannel));
												currentId++;
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

									onNotifyInitialStateHandlers.Add(SyntaxFactory.ParseStatement($"base.SyncNetworkState(peer);"));

									MethodDeclarationSyntax onNotifyInitialStateUpdate = SyntaxFactory
										.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "SyncNetworkState")
										.WithModifiers(
											SyntaxFactory.TokenList(
												SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
												SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
											)
										).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
											new ParameterSyntax[] {
											SyntaxFactory.Parameter(SyntaxFactory.Identifier("NetworkPeer peer"))
										})))
										.WithBody(SyntaxFactory.Block(SyntaxFactory.List(onNotifyInitialStateHandlers)));

									networkVariablesRegister.Add(SyntaxFactory.ParseStatement("base.___RegisterNetworkVariables___();"));

									MethodDeclarationSyntax onRegisterNetworkVariables = SyntaxFactory
										.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "___RegisterNetworkVariables___")
										.WithModifiers(
											SyntaxFactory.TokenList(
												SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
												SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
											)
										).WithBody(SyntaxFactory.Block(SyntaxFactory.List(networkVariablesRegister)));

									parentClass = parentClass.AddMembers(
										onServerPropertyChanged,
										onClientPropertyChanged,
										onPropertyChanged,
										onNotifyCollectionChange,
										onNotifyInitialStateUpdate,
										onRegisterNetworkVariables
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

									context.AddSource($"{parentClass.Identifier.Text}_netvar_generated_code_.cs", Source.Clean(sb.ToString()));
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

		private string DetermineIsServerValue(bool isNetworkBehaviour, bool isClientBehaviour, bool isServerBehaviour, bool isDualBehaviour, out string baseClassName)
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
				else if (isDualBehaviour)
				{
					baseClassName = "DualBehaviour";
					return "NetworkManager.IsServerActive";
				}
			}

			throw new NotImplementedException("Unable to determine 'isServer' value");
		}

		private MethodDeclarationSyntax CreateInvokeMethod(string propertyName, string propertyId, string baseClassName, ImmutableArray<ITypeSymbol> args)
		{
			var argumentList = new SeparatedSyntaxList<ParameterSyntax>();
			var defaultArgs = new ParameterSyntax[]
			{
				SyntaxFactory.Parameter(SyntaxFactory.Identifier($"options"))
					.WithType(SyntaxFactory.ParseTypeName("NetworkVariableOptions"))
					.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))),
				SyntaxFactory.Parameter(SyntaxFactory.Identifier($"invokeLocally"))
					.WithType(SyntaxFactory.ParseTypeName("bool"))
					.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
			};

			var statements = new List<StatementSyntax>
			{
				SyntaxFactory.ParseStatement($"options ??= DefaultNetworkVariableOptions;"),
				SyntaxFactory.ParseStatement("using var buffer = NetworkManager.Pool.Rent(enableTracking: false);")
			};

			#region Invoke Arguments
			for (int i = 0; i < args.Length; i++)
			{
				ITypeSymbol arg = args[i];
				var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier($"arg{i}"))
					.WithType(SyntaxFactory.ParseTypeName(arg.ToString()));

				argumentList = argumentList.Add(parameter);
				statements.Add(SyntaxFactory.ParseStatement($"buffer.WriteAsBinary({parameter.Identifier.Text});"));
			}

			argumentList = argumentList.AddRange(defaultArgs);
			#endregion

			#region Method Body
			statements.Add(
				baseClassName == "NetworkBehaviour"
					? SyntaxFactory
						.IfStatement(
							SyntaxFactory.ParseExpression("IsClient"),
							SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Client.NetworkVariableSync(buffer, {propertyId}, options);"))
						)
						.WithElse(
							SyntaxFactory.ElseClause(
								SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Server.NetworkVariableSync(buffer, {propertyId}, options);"))
							)
						)
					: baseClassName == "ServerBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSync(buffer, {propertyId}, options);")
					: baseClassName == "ClientBehaviour" ? SyntaxFactory.ParseStatement($"Client.NetworkVariableSync(buffer, {propertyId}, options);")
					: baseClassName == "DualBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSync(buffer, {propertyId}, options);")
					: GenHelper.EmptyStatement());

			statements.Add(SyntaxFactory.IfStatement(
				SyntaxFactory.ParseExpression("invokeLocally"),
				SyntaxFactory.ParseStatement($"m_{propertyName}?.Invoke({string.Join(", ", Enumerable.Range(0, args.Length).Select(i => $"arg{i}"))});"))
			);
			#endregion

			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"{propertyName}")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
				.WithParameterList(
					SyntaxFactory.ParameterList(argumentList)
				)
				.WithBody(SyntaxFactory.Block(statements));
		}

		private MethodDeclarationSyntax CreateSyncMethod(string propertyName, string propertyId, string baseClassName)
		{
			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"Sync{propertyName}")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
				.WithParameterList(
					SyntaxFactory.ParameterList(
						SyntaxFactory.SeparatedList(
							new ParameterSyntax[]
							{
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"options")).WithType(SyntaxFactory.ParseTypeName("NetworkVariableOptions")).WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))),
							}

						)
					)
				)
				.WithBody(
					SyntaxFactory.Block(
						SyntaxFactory.ParseStatement($"options ??= {propertyName}Options ?? DefaultNetworkVariableOptions;"),
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
							: baseClassName == "DualBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSync({propertyName}, {propertyId}, options);")
							: GenHelper.EmptyStatement()
					)
				);
		}

		private StatementSyntax CreateSyncInitialState(string propertyName, string propertyId, string baseClassName) // Only Server
		{
			return baseClassName == "NetworkBehaviour"
				 ? SyntaxFactory
					 .IfStatement(
						 SyntaxFactory.ParseExpression("IsServer"),
						 SyntaxFactory.Block(SyntaxFactory.ParseStatement($"Server.NetworkVariableSyncToPeer({propertyName}, {propertyId}, peer);"))
					 )
				 : baseClassName == "ServerBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSyncToPeer({propertyName}, {propertyId}, peer);")
				 : baseClassName == "DualBehaviour" ? SyntaxFactory.ParseStatement($"Server.NetworkVariableSyncToPeer({propertyName}, {propertyId}, peer);")
				 : GenHelper.EmptyStatement();
		}

		private StatementSyntax CreateRegisterNetworkVariable(string propertyName, byte propertyId, bool requiresOwnership,
			bool isClientAuthority, bool checkEquality, string deliveryMode, string target, byte sequenceChannel) // Only Server
		{
			return SyntaxFactory.ParseStatement(
				$"___RegisterNetworkVariable___(\"{propertyName}\", {propertyId}, {(requiresOwnership ? "true" : "false")}, {(isClientAuthority ? "true" : "false")}, {(checkEquality ? "true" : "false")}, {deliveryMode}, {target}, {sequenceChannel});"
			);
		}

		private MethodDeclarationSyntax CreatePartialHandler(string propertyName, TypeSyntax type)
		{
			return SyntaxFactory
				.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"On{propertyName}Changed")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
				.WithParameterList(
					SyntaxFactory.ParameterList(
						SyntaxFactory.SeparatedList(
							new ParameterSyntax[]
							{
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"prev{propertyName}")).WithType(type),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"next{propertyName}")).WithType(type),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier("isWriting")).WithType(SyntaxFactory.ParseTypeName("bool"))
							}
						)
					)
				).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
		}

		private MethodDeclarationSyntax CreateVirtualHandler(string propertyName, TypeSyntax type)
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
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"prev{propertyName}")).WithType(type),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier($"next{propertyName}")).WithType(type),
								SyntaxFactory.Parameter(SyntaxFactory.Identifier("isWriting")).WithType(SyntaxFactory.ParseTypeName("bool"))
							}
						)
					)
				).WithBody(SyntaxFactory.Block());
		}

		private SwitchSectionSyntax CreateDelegateSection(
			string caseExpression, // propertyId
			string propertyName,
			string propertyType,
			bool isSerializable,
			bool isSerializableWithPeer,
			string isServer,
			ImmutableArray<ITypeSymbol> args
		)
		{
			var statements = new List<StatementSyntax>()
			{
				SyntaxFactory.ParseStatement($"propertyName = \"{propertyName}\";"),
			};

			for (int i = 0; i < args.Length; i++)
			{
				statements.Add(SyntaxFactory.ParseStatement($"var arg{i} = buffer.ReadAsBinary<{args[i]}>();"));
			}

			statements.Add(SyntaxFactory.ParseStatement($"m_{propertyName}?.Invoke({string.Join(", ", Enumerable.Range(0, args.Length).Select(i => $"arg{i}"))});"));
			statements.Add(SyntaxFactory.BreakStatement());

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
							SyntaxFactory.Block(statements),
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
								SyntaxFactory.BreakStatement()
							),
						}
					)
				);
		}

		private SwitchSectionSyntax CreateSection(
			string caseExpression, // propertyId
			string propertyName,
			string propertyType,
			bool isSerializable,
			bool isSerializableWithPeer,
			string isServerBroadcastsClientUpdates,
			string determinedValue
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
								SyntaxFactory.IfStatement(
									SyntaxFactory.ParseExpression($"!OnNetworkVariableDeepEquals(m_{propertyName}, nextValue, propertyName, {caseExpression})"),
									SyntaxFactory.Block(
										SyntaxFactory.IfStatement(
											SyntaxFactory.ParseExpression($"{determinedValue} && {isServerBroadcastsClientUpdates}"),
											SyntaxFactory.Block(
												SyntaxFactory.ParseStatement($"Sync{propertyName}();")
											)
										),
										SyntaxFactory.ParseStatement($"On{propertyName}Changed(m_{propertyName}, nextValue, false);"),
										SyntaxFactory.ParseStatement($"OnBase{propertyName}Changed(m_{propertyName}, nextValue, false);"),
										(propertyType.StartsWith("ObservableDictionary") || propertyType.StartsWith("ObservableList"))
											? SyntaxFactory.ParseStatement("nextValue.OnUpdate?.Invoke(false);")
											: GenHelper.EmptyStatement(),
										SyntaxFactory.ParseStatement($"m_{propertyName} = nextValue;")
									),
									SyntaxFactory.ElseClause(
										SyntaxFactory.Block(
											SyntaxFactory.ParseStatement("return;")
										)
									)
								),
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
								SyntaxFactory.ParseStatement("using var nBuffer = NetworkManager.Pool.Rent(enableTracking: false);"),
								SyntaxFactory.ParseStatement("nBuffer.Write(buffer.GetSpan().Slice(0, buffer.Length)); // from current position to end > skip propertyId(header)"),
								SyntaxFactory.ParseStatement($"nBuffer.SeekToBegin();"),
								isSerializableWithPeer
									? SyntaxFactory.ParseStatement($"var nextValue = nBuffer.Deserialize<{propertyType}>(peer != null ? peer : NetworkManager.LocalPeer, {determinedValue});")
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
														SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("255")),
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
