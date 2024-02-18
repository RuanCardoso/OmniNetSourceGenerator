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
	internal class NetVarGenerator : ISourceGenerator
	{
		protected readonly HashSet<string> m_NetVarSupportedTypes = new HashSet<string>()
		{
			//"int",
			//"float",
			//"Tipo1"
		};

		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is NetVarSyntaxReceiver netVarSyntaxReceiver)
				{
					if (netVarSyntaxReceiver.fieldDeclarationSyntaxes.Any())
					{
						var classWithFields = netVarSyntaxReceiver.fieldDeclarationSyntaxes.GroupBy(x => (ClassDeclarationSyntax)x.Parent);
						foreach (var classWithField in classWithFields)
						{
							ClassDeclarationSyntax originalClassDeclarationSyntax = classWithField.Key;
							StringBuilder sb = new StringBuilder();
							#region Usings
							sb.AppendLine("using Newtonsoft.Json;");
							sb.AppendLine("using MessagePack;");
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

									// Deserialize Func switch case
									List<SwitchSectionSyntax> statementSyntaxes = new List<SwitchSectionSyntax>();
									MethodDeclarationSyntax DeserializeDeclarationSyntax() => SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "___2205032023")
										.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
										.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new ParameterSyntax[] {
											SyntaxFactory.Parameter(SyntaxFactory.Identifier("byte id")),
											SyntaxFactory.Parameter(SyntaxFactory.Identifier("IDataReader dataReader"))
										}))).WithBody(SyntaxFactory.Block(SyntaxFactory.SwitchStatement(
									  SyntaxFactory.ParseExpression("id"),
									  SyntaxFactory.List(statementSyntaxes)
									)));

									int netVarId = 0;
									foreach (var fieldDeclarationSyntax in classWithField)
									{
										var variables = fieldDeclarationSyntax.Declaration.Variables;
										foreach (var variable in variables)
										{
											string fieldName = variable.Identifier.ValueText;
											if (fieldName.Contains("M_") || char.IsUpper(fieldName[0]))
											{
												context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CB005", "Omni", "NetVar fields must always begin with the first lowercase letter.", "", DiagnosticSeverity.Error, true), Location.None));
												continue;
											}

											netVarId++;
											if (netVarId > byte.MaxValue)
												continue;

											// Symbol Info/Type Info
											TypeSyntax declarationType = fieldDeclarationSyntax.Declaration.Type;
											TypeInfo typeInfo = semanticModel.GetTypeInfo(declarationType);

											// Json serialize?
											bool serializeAsJson = false;
											var attributeSyntaxes = fieldDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.ArgumentList != null && y.ArgumentList.Arguments.Count > 0 && y.Name.ToString() == "NetVar"));
											if (attributeSyntaxes.Any())
											{
												foreach (var attributeSyntax in attributeSyntaxes)
												{
													var arguments = attributeSyntax.ArgumentList.Arguments;
													var serializeAsJsonExpression = arguments.First(x => x.NameEquals.Name.Identifier.Text == "SerializeAsJson");
													if (bool.TryParse(((LiteralExpressionSyntax)serializeAsJsonExpression.Expression).Token.ValueText, out bool serializeAsJsonValue))
													{
														serializeAsJson = serializeAsJsonValue;
													}
												}
											}

											string propertyName = fieldName.Replace("m_", "");
											propertyName = char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1);
											// Make serializer settings
											PropertyDeclarationSyntax serializerSettingsSyntax = SyntaxFactory.PropertyDeclaration(
											SyntaxFactory.ParseTypeName(serializeAsJson ? "JsonSerializerSettings" : "MessagePackSerializerOptions"), $"{propertyName}SerializerSettings").WithAccessorList(
											SyntaxFactory.AccessorList(
												SyntaxFactory.List(new[]
												{
													SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
													.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
													SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
													.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
												})
											)).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
											.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseName(serializeAsJson ? "new();" : "MessagePackSerializer.DefaultOptions;")));
											// Make the property
											var property = SyntaxFactory.PropertyDeclaration(declarationType, propertyName)
											.WithModifiers(fieldDeclarationSyntax.Modifiers).WithAccessorList(
											SyntaxFactory.AccessorList(
											SyntaxFactory.List(new[]
											{
											   SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
												 .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {fieldName};"))),
											   SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
												 .WithBody(SyntaxFactory.Block(
													 SyntaxFactory.ParseStatement($"{fieldName} = value;"),
													 SyntaxFactory.ParseStatement($"IDataWriter writer = GetWriter();").WithLeadingTrivia(SyntaxFactory.Comment("// Let's serialize the data and send it to the network.")),
													 SyntaxFactory.ParseStatement(m_NetVarSupportedTypes.Contains(declarationType.ToString()) ? $"writer.Write({fieldName});" : $"{(serializeAsJson ? $"writer.SerializeWithJsonNet({fieldName}, {serializerSettingsSyntax.Identifier.ValueText});" : $"writer.SerializeWithMsgPack({fieldName}, {serializerSettingsSyntax.Identifier.ValueText});")}"),
													 SyntaxFactory.ParseStatement($"___2205032024(writer, {netVarId});"),
													 SyntaxFactory.ParseStatement("Release(writer);")
												 )).WithTrailingTrivia(SyntaxFactory.Comment($"// IsUnmanagedType: {typeInfo.ConvertedType.IsUnmanagedType} | Type: {declarationType} | SerializeAsJson: {serializeAsJson}")),
											})));

											statementSyntaxes.Add(SyntaxFactory.SwitchSection(
											SyntaxFactory.List(new SwitchLabelSyntax[] {
												SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(netVarId.ToString()))
											}),
											SyntaxFactory.List(new StatementSyntax[] {
												SyntaxFactory.Block(
												 SyntaxFactory.ParseStatement(m_NetVarSupportedTypes.Contains(declarationType.ToString()) ? $"{fieldName} = dataReader.Read{char.ToUpperInvariant(declarationType.ToString()[0]) + declarationType.ToString().Substring(1)}();" : $"{(serializeAsJson ? $"{fieldName} = dataReader.DeserializeWithJsonNet<{declarationType}>({serializerSettingsSyntax.Identifier.ValueText});" : $"{fieldName} = dataReader.DeserializeWithMsgPack<{declarationType}>({serializerSettingsSyntax.Identifier.ValueText});")}"),
												SyntaxFactory.BreakStatement())
											})));

											// Add property!
											newClassDeclarationSyntax = newClassDeclarationSyntax.AddMembers(serializerSettingsSyntax, property.WithLeadingTrivia(SyntaxFactory.Comment("\n")));
										}
									}

									newClassDeclarationSyntax = newClassDeclarationSyntax.AddMembers(DeserializeDeclarationSyntax());
									newNamespaceDeclarationSyntax = newNamespaceDeclarationSyntax.AddMembers(newClassDeclarationSyntax);
									if (originalNamespaceDeclarationSyntax == null) sb.Append(newClassDeclarationSyntax.NormalizeWhitespace().ToString());
									else sb.Append(newNamespaceDeclarationSyntax.NormalizeWhitespace().ToString());
									context.AddSource($"{originalClassDeclarationSyntax.Identifier.ToFullString()}_netvar_gen_code.cs", sb.ToString());
								}
								else
								{
									context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CB004", "Omni", "The class must inherit from NetworkBehaviour.", "", DiagnosticSeverity.Error, true), Location.None));
								}
							}
							else
							{
								context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CB003", "Omni", "The class must contain the 'partial' keyword.", "", DiagnosticSeverity.Error, true), Location.None));
							}
						}
					}
					else
					{
						// context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CB002", "Omni", "No component inheriting from NetworkBehaviour was found.", "", DiagnosticSeverity.Warning, true), Location.None));
					}
				}
			}
			catch (Exception ex)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CB001", "Omni", ex.ToString(), "", DiagnosticSeverity.Error, true), Location.None));
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new NetVarSyntaxReceiver());
		}
	}

	internal class NetVarSyntaxReceiver : ISyntaxReceiver
	{
		internal List<FieldDeclarationSyntax> fieldDeclarationSyntaxes = new List<FieldDeclarationSyntax>();
		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax)
			{
				if (fieldDeclarationSyntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "NetVar")))
				{
					fieldDeclarationSyntaxes.Add(fieldDeclarationSyntax);
				}
			}
		}
	}
}