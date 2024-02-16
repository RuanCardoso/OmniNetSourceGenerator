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
	internal class SyncVarGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			try
			{
				if (context.SyntaxReceiver is SyncVarSyntaxReceiver syncVarSyntaxReceiver)
				{
					if (syncVarSyntaxReceiver.FieldDeclarationSyntaxes.Any())
					{
						var classGroups = syncVarSyntaxReceiver.FieldDeclarationSyntaxes.GroupBy(x => x.ClassDeclarationSyntax);
						foreach (var classGroup in classGroups)
						{
							ClassDeclarationSyntax classDeclarationSyntax = classGroup.Key;
							List<string> deserializeMethods = new List<string>();

							StringBuilder classBuilder = new StringBuilder();
							StringBuilder fieldBuilder = new StringBuilder();

							int netVarId = 0;
							string @class = classDeclarationSyntax.GetClassName();
							string @namespace = classDeclarationSyntax.GetNamespaceName();
							string methodName = $"__{Math.Abs(@class.GetHashCode())}";

							foreach (var OmniFieldDeclarationSyntax in classGroup)
							{
								bool serializeAsJson = false;
								bool verifyIfEqual = false;
								// Get the parameters of attribute.
								FieldDeclarationSyntax fieldDeclarationSyntax = OmniFieldDeclarationSyntax.FieldDeclarationSyntax;
								IEnumerable<AttributesWithMultipleParameters> attributes = fieldDeclarationSyntax.GetAttributesWithMultipleParameters(context.GetSemanticModel(classDeclarationSyntax.SyntaxTree), "NetVar");
								if (attributes != null && attributes.Any())
								{
									AttributesWithMultipleParameters attr = attributes.First(); // Get the first attribute. only one attribute per field is allowed!
									if (attr.ParametersByName.TryGetValue("SerializeAsJson", out AttributesWithMultipleParameters.Parameter p0))
									{
										if (bool.TryParse(p0.Value, out bool result))
										{
											serializeAsJson = result;
										}
									}

									if (attr.ParametersByName.TryGetValue("VerifyIfEqual", out AttributesWithMultipleParameters.Parameter p1))
									{
										if (bool.TryParse(p1.Value, out bool result))
										{
											verifyIfEqual = result;
										}
									}
								}

								// Process the field.
								var fieldSyntaxes = fieldDeclarationSyntax.GetFieldVariables();
								foreach (var fieldSyntax in fieldSyntaxes)
								{
									if (netVarId >= byte.MaxValue)
										continue;

									MemberInfo fieldInfo = fieldSyntax.GetFieldInfo(context.GetSemanticModel(fieldSyntax.SyntaxTree), true);
									string fieldName = fieldInfo.Name.Replace("m_", "");
									string propertyName = $"{char.ToUpperInvariant(fieldName[0])}{fieldName.Substring(1)}";

									// Check if is a collection (:
									bool isCollection = false;
									if (fieldInfo.TypeName != "string")
									{
										if (fieldInfo.TypeKind == TypeKind.Array)
										{
											isCollection = true;
										}
										else
										{
											foreach (var @interface in fieldInfo.Interfaces)
											{
												string name = @interface.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
												if (name.Contains("IEnumerable<"))
												{
													isCollection = true;
													break;
												}
											}
										}
									}

									// Create the property
									fieldBuilder.AppendLine($"\t\tprivate {fieldInfo.TypeName} {propertyName}");
									fieldBuilder.AppendLine("\t\t{");
									fieldBuilder.AppendLine("\t\t\tget");
									fieldBuilder.AppendLine("\t\t\t{");
									fieldBuilder.AppendLine($"\t\t\t\treturn {fieldInfo.Name};");
									fieldBuilder.AppendLine("\t\t\t}");
									fieldBuilder.AppendLine("\t\t\tset");
									fieldBuilder.AppendLine("\t\t\t{");
									if (verifyIfEqual) fieldBuilder.AppendLine($"\t\t\t\tif(!{fieldInfo.Name}.{(isCollection ? "SequenceEqual" : "Equals")}(value))"); //  Equals -> IEquatable<T>, SequenceEqual -> IEqualityComparer<T>, if not implemented, use the default object behaviour Equals or object GetHashCode.
									else fieldBuilder.AppendLine("\t\t\t\t// CHECK -> Disabled");
									fieldBuilder.AppendLine("\t\t\t\t{");
									fieldBuilder.AppendLine($"\t\t\t\t\t{fieldInfo.TypeName} oldValue = {fieldInfo.Name};");
									fieldBuilder.AppendLine($"\t\t\t\t\t{fieldInfo.Name} = value;");
									fieldBuilder.AppendLine($"\t\t\t\t\t{methodName}<{fieldInfo.TypeName}>(oldValue, value, \"{fieldInfo.TypeName}\", \"{fieldInfo.Name}\", {++netVarId}, {fieldInfo.IsPrimitiveType.ToString().ToLowerInvariant()}, {fieldInfo.IsValueType.ToString().ToLowerInvariant()}, \"{fieldInfo.TypeKind}\", {serializeAsJson.ToString().ToLowerInvariant()});");
									fieldBuilder.AppendLine("\t\t\t\t}");
									fieldBuilder.AppendLine("\t\t\t}");
									fieldBuilder.AppendLine("\t\t}");

									// Generate CASE OF deserialize func.
									deserializeMethods.Add($"\t\t\t\tcase {netVarId}: // {fieldInfo.Name}");
									deserializeMethods.Add("\t\t\t\t{");
									if (!serializeAsJson) deserializeMethods.Add($"\t\t\t\t\t{fieldInfo.Name} = dataReader.DeserializeWithMsgPack<{fieldInfo.TypeName}>();");
									else deserializeMethods.Add($"\t\t\t\t\t{fieldInfo.Name} = dataReader.DeserializeWithJsonNet<{fieldInfo.TypeName}>();");
									deserializeMethods.Add("\t\t\t\t}");
									deserializeMethods.Add("\t\t\t\tbreak;");
								}
							}

							var usings = classDeclarationSyntax.GetAllUsingsDirective().Select(x => $"using {x.Name};").ToList();
							if (!usings.Contains("using System.Linq;")) usings.Add("using System.Linq;");
							classBuilder.AppendLine(Helpers.CreateNamespace(@namespace, usings, () =>
							{
								return Helpers.CreateClass("partial", @class, "NetworkBehaviour", OnCreated: () =>
								{
									StringBuilder codeBuilder = new StringBuilder();
									codeBuilder.AppendLine("");
									codeBuilder.AppendLine($"\t\tprivate void {methodName}<T>(T oldValue, T newValue, string type, string netVarName, int id, bool isPrimitiveType, bool isValueType, string typeKind, bool serializeAsJson)");
									codeBuilder.AppendLine("\t\t{");
									codeBuilder.AppendLine("\t\t\tif(OnPropertyChanged(netVarName, id))");
									codeBuilder.AppendLine("\t\t\t\t___2205032024<T>(oldValue, newValue, type, netVarName, id, isPrimitiveType, isValueType, typeKind, serializeAsJson);");
									codeBuilder.AppendLine("\t\t}");

									// Implements Deserialize Method
									codeBuilder.AppendLine("\t\tprotected override void ___2205032023(byte id, IDataReader dataReader)");
									codeBuilder.AppendLine("\t\t{");
									codeBuilder.AppendLine("\t\t\tswitch(id)");
									codeBuilder.AppendLine("\t\t\t{");
									foreach (string deserializeMethod in deserializeMethods)
									{
										codeBuilder.AppendLine(deserializeMethod);
									}
									codeBuilder.AppendLine("\t\t\t}");
									codeBuilder.AppendLine("\t\t}");
									// Finish
									codeBuilder.AppendLine(fieldBuilder.ToString());
									return codeBuilder.ToString();
								});
							}));
							context.AddSource($"{@class}__syncvar_g", classBuilder.ToString().Trim());
						}
					}
				}
			}
			catch (Exception ex)
			{
				Helpers.Log("SyncVarGen", ex.ToString());
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyncVarSyntaxReceiver());
		}

		internal class SyncVarSyntaxReceiver : ISyntaxReceiver
		{
			internal class OmniFieldDeclarationSyntax
			{
				public OmniFieldDeclarationSyntax(FieldDeclarationSyntax fieldDeclarationSyntax, NamespaceDeclarationSyntax namespaceDeclarationSyntax, ClassDeclarationSyntax classDeclarationSyntax)
				{
					FieldDeclarationSyntax = fieldDeclarationSyntax;
					NamespaceDeclarationSyntax = namespaceDeclarationSyntax;
					ClassDeclarationSyntax = classDeclarationSyntax;
				}

				internal FieldDeclarationSyntax FieldDeclarationSyntax { get; }
				internal NamespaceDeclarationSyntax NamespaceDeclarationSyntax { get; }
				internal ClassDeclarationSyntax ClassDeclarationSyntax { get; }
			}

			public List<OmniFieldDeclarationSyntax> FieldDeclarationSyntaxes { get; } = new List<OmniFieldDeclarationSyntax>();
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode.TryGetDeclarationSyntax(out FieldDeclarationSyntax fieldDeclarationSyntax))
				{
					if (fieldDeclarationSyntax.HasAttribute("NetVar"))
					{
						ClassDeclarationSyntax classDeclarationSyntax = fieldDeclarationSyntax.GetClassDeclarationSyntax();
						if (classDeclarationSyntax.HasModifier(SyntaxKind.PartialKeyword) && !fieldDeclarationSyntax.HasModifier(SyntaxKind.StaticKeyword))
						{
							FieldDeclarationSyntaxes.Add(new OmniFieldDeclarationSyntax(fieldDeclarationSyntax, fieldDeclarationSyntax.GetNamespaceDeclarationSyntax(), classDeclarationSyntax));
						}
					}
				}
			}
		}
	}
}
