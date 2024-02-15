using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Utils
{
	internal class MemberInfo
	{
		internal MemberInfo(string name, string value, string typeName, bool isPrimitiveType, bool isValueType, TypeKind typeKind)
		{
			Name = name;
			Value = value;
			TypeName = typeName;
			IsPrimitiveType = isPrimitiveType;
			IsValueType = isValueType;
			TypeKind = typeKind;
		}

		internal string Name { get; set; }
		internal string Value { get; set; }
		internal string TypeName { get; set; }
		internal bool IsPrimitiveType { get; set; }
		internal bool IsValueType { get; set; }
		internal TypeKind TypeKind { get; set; }
	}

	internal class AttributeWithMultipleParameters
	{
		internal class Parameter
		{
			internal Parameter(string name, string value, string typeName)
			{
				Name = name;
				Value = value;
				TypeName = typeName;
			}

			internal string Name { get; }
			internal string Value { get; }
			internal string TypeName { get; }
		}

		internal List<Parameter> Parameters { get; } = new List<Parameter>();
		internal Dictionary<string, Parameter> ParametersByName { get; } = new Dictionary<string, Parameter>();
	}

	internal static class Extensions
	{
		private static void ThrowAnErrorIfIsNull<T>(T value)
		{
			if (value == null)
				throw new NullReferenceException($"NullReferenceException -> The type {typeof(T).Name} is null!");
		}

		public static SemanticModel GetSemanticModel(this GeneratorExecutionContext context, SyntaxTree syntaxTree)
		{
			ThrowAnErrorIfIsNull(context);
			ThrowAnErrorIfIsNull(syntaxTree);
			return context.Compilation.GetSemanticModel(syntaxTree);
		}

		public static bool TryGetDeclarationSyntax<T>(this SyntaxNode syntaxNode, out T declarationSyntax) where T : SyntaxNode
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			declarationSyntax = default;
			if (syntaxNode is T @class)
			{
				declarationSyntax = @class;
				return true;
			}
			return false;
		}

		public static IEnumerable<T> GetDescendantDeclarationSyntax<T>(this SyntaxTree syntaxTree) where T : SyntaxNode
		{
			ThrowAnErrorIfIsNull(syntaxTree);
			if (syntaxTree.TryGetRoot(out SyntaxNode root))
			{
				var nodes = root.DescendantNodes().OfType<T>();
				foreach (SyntaxNode syntaxNode in nodes)
				{
					yield return syntaxNode as T;
				}
			}
		}

		public static IEnumerable<T> GetAncestorsDeclarationSyntax<T>(this SyntaxTree syntaxTree) where T : SyntaxNode
		{
			ThrowAnErrorIfIsNull(syntaxTree);
			if (syntaxTree.TryGetRoot(out SyntaxNode root))
			{
				var nodes = root.Ancestors().OfType<T>();
				foreach (SyntaxNode syntaxNode in nodes)
				{
					yield return syntaxNode as T;
				}
			}
		}

		public static IEnumerable<T> GetDescendantDeclarationSyntax<T>(this GeneratorExecutionContext context) where T : SyntaxNode
		{
			ThrowAnErrorIfIsNull(context);
			foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
			{
				if (syntaxTree.TryGetRoot(out SyntaxNode root))
				{
					var nodes = root.DescendantNodes().OfType<T>();
					foreach (SyntaxNode syntaxNode in nodes)
					{
						yield return syntaxNode as T;
					}
				}
				else continue;
			}
		}

		public static IEnumerable<T> GetAncestorsDeclarationSyntax<T>(this GeneratorExecutionContext context) where T : SyntaxNode
		{

			ThrowAnErrorIfIsNull(context);
			foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
			{
				if (syntaxTree.TryGetRoot(out SyntaxNode root))
				{
					var nodes = root.Ancestors().OfType<T>();
					foreach (SyntaxNode syntaxNode in nodes)
					{
						yield return syntaxNode as T;
					}
				}
				else continue;
			}
		}

		public static SeparatedSyntaxList<VariableDeclaratorSyntax> GetFieldVariables(this FieldDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Declaration.Variables;
		}

		public static string GetIdentifierName(this MethodDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Identifier.Text;
		}

		public static string GetIdentifierName(this ClassDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Identifier.Text;
		}

		public static string GetIdentifierName(this NamespaceDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Name.ToString();
		}

		public static TypeSyntax GetDescendantTypeSyntax(SyntaxNode node)
		{
			return node.DescendantNodes().OfType<TypeSyntax>().First();
		}

		public static TypeSyntax GetAncestorsTypeSyntax(SyntaxNode node)
		{
			return node.Ancestors().OfType<TypeSyntax>().First();
		}

		public static MemberInfo GetFieldInfo(this VariableDeclaratorSyntax syntaxNode, SemanticModel semanticModel)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			EqualsValueClauseSyntax equalsValueClauseSyntax = syntaxNode.Initializer;
			string identifierName = syntaxNode.Identifier.Text;
			string initializerValue = equalsValueClauseSyntax == null ? "" : equalsValueClauseSyntax.Value is LiteralExpressionSyntax literalExpressionSyntax ? literalExpressionSyntax.Token.ValueText : equalsValueClauseSyntax.Value.ToString();
			var typeSymbol = equalsValueClauseSyntax == null ? semanticModel.GetTypeInfo(GetDescendantTypeSyntax(syntaxNode.Parent)).ConvertedType : semanticModel.GetTypeInfo(equalsValueClauseSyntax.Value).ConvertedType;
			return new MemberInfo(identifierName, initializerValue, typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), typeSymbol.IsUnmanagedType, typeSymbol.IsValueType, typeSymbol.TypeKind);
		}

		public static MemberInfo GetPropertyInfo(this PropertyDeclarationSyntax syntaxNode, SemanticModel semanticModel)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			EqualsValueClauseSyntax equalsValueClauseSyntax = syntaxNode.Initializer;
			string identifierName = syntaxNode.Identifier.Text;
			string initializerValue = equalsValueClauseSyntax == null ? "" : equalsValueClauseSyntax.Value is LiteralExpressionSyntax literalExpressionSyntax ? literalExpressionSyntax.Token.ValueText : equalsValueClauseSyntax.Value.ToString();
			var typeSymbol = equalsValueClauseSyntax == null ? semanticModel.GetTypeInfo(GetDescendantTypeSyntax(syntaxNode.Parent)).ConvertedType : semanticModel.GetTypeInfo(equalsValueClauseSyntax.Value).ConvertedType;
			return new MemberInfo(identifierName, initializerValue, typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), typeSymbol.IsUnmanagedType, typeSymbol.IsValueType, typeSymbol.TypeKind);
		}

		public static NamespaceDeclarationSyntax GetNamespaceDeclarationSyntax(this SyntaxNode syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode is NamespaceDeclarationSyntax self
				? self
				: syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax
				? namespaceDeclarationSyntax
				: syntaxNode.Parent?.GetNamespaceDeclarationSyntax();
		}

		public static string GetNamespaceName(this SyntaxNode syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			NamespaceDeclarationSyntax namespaceDeclarationSyntax = syntaxNode.GetNamespaceDeclarationSyntax();
			return namespaceDeclarationSyntax != null ? namespaceDeclarationSyntax.GetIdentifierName() : "";
		}

		public static ClassDeclarationSyntax GetClassDeclarationSyntax(this SyntaxNode syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode is ClassDeclarationSyntax self
				? self
				: syntaxNode.Parent is ClassDeclarationSyntax classDeclarationSyntax
				? classDeclarationSyntax
				: syntaxNode.Parent?.GetClassDeclarationSyntax();
		}

		public static string GetClassName(this SyntaxNode syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ClassDeclarationSyntax classDeclarationSyntax = syntaxNode.GetClassDeclarationSyntax();
			return classDeclarationSyntax != null ? classDeclarationSyntax.GetIdentifierName() : "";
		}

		public static bool HasModifier(this MemberDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this PropertyDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this ClassDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this MethodDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this NamespaceDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this FieldDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		private static bool Internal_HasAttribute(MemberDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			foreach (AttributeListSyntax listSyntax in syntaxNode.AttributeLists)
			{
				foreach (var attributeSyntax in listSyntax.Attributes)
				{
					if (attributeSyntax.Name is IdentifierNameSyntax identifierNameSyntax)
					{
						if (identifierNameSyntax.Identifier.Text == attrName)
						{
							return true;
						}
						else continue;
					}
					else continue;
				}
			}
			return false;
		}

		public static bool HasAttribute(this MethodDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this ClassDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this PropertyDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this FieldDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		private static IEnumerable<AttributeWithMultipleParameters> Internal_GetAttributesWithMultipleParameters(this MemberDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			foreach (AttributeListSyntax listSyntax in syntaxNode.AttributeLists)
			{
				foreach (var attributeSyntax in listSyntax.Attributes)
				{
					if (attributeSyntax.Name is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.Text == attrName)
					{
						AttributeWithMultipleParameters attributeWithMultipleParameters = new AttributeWithMultipleParameters();
						if (attributeSyntax.ArgumentList != null)
						{
							int pNameIndex = 0;
							foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
							{
								string pName = attributeArgumentSyntax.NameColon?.Name.Identifier.Text ?? attributeArgumentSyntax.NameEquals?.Name.Identifier.Text ?? $"p{pNameIndex++}";
								string pTypeName = semanticModel.GetTypeInfo(attributeArgumentSyntax.Expression).ConvertedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
								string pValue = attributeArgumentSyntax.Expression is LiteralExpressionSyntax literalExpressionSyntax ? literalExpressionSyntax.Token.ValueText : attributeArgumentSyntax.Expression.ToString();
								attributeWithMultipleParameters.Parameters.Add(new AttributeWithMultipleParameters.Parameter(pName, pValue, pTypeName));
								attributeWithMultipleParameters.ParametersByName.Add(pName, new AttributeWithMultipleParameters.Parameter(pName, pValue, pTypeName));
							}
						}
						else continue;
						yield return attributeWithMultipleParameters;
					}
					else continue;
				}
			}
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this ClassDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			return syntaxNode.Internal_GetAttributesWithMultipleParameters(semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this PropertyDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			return syntaxNode.Internal_GetAttributesWithMultipleParameters(semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this MemberDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			return syntaxNode.Internal_GetAttributesWithMultipleParameters(semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this MethodDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			return syntaxNode.Internal_GetAttributesWithMultipleParameters(semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this FieldDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			ThrowAnErrorIfIsNull(semanticModel);
			return syntaxNode.Internal_GetAttributesWithMultipleParameters(semanticModel, attrName);
		}

		private static IEnumerable<string> Internal_GetAttributesWithSingleParameter(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			foreach (AttributeListSyntax listSyntax in syntaxNode.AttributeLists)
			{
				foreach (var attributeSyntax in listSyntax.Attributes)
				{
					if (attributeSyntax.Name is IdentifierNameSyntax identifierNameSyntax)
					{
						if (identifierNameSyntax.Identifier.Text == attrName)
						{
							if (attributeSyntax.ArgumentList != null)
							{
								foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
								{
									if (attributeArgumentSyntax.Expression is LiteralExpressionSyntax literalExpressionSyntax)
									{
										// Se o argumento for um literal, adiciona o valor à lista
										yield return literalExpressionSyntax.Token.ValueText;
									}
									else
									{
										// Se o argumento não for um literal, você pode ajustar essa lógica conforme necessário
										// Aqui, estamos adicionando a representação em string da expressão
										yield return attributeArgumentSyntax.Expression.ToString();
									}
								}
							}
							else continue;
						}
						else continue;
					}
					else continue;
				}
			}
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this FieldDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Internal_GetAttributesWithSingleParameter(attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this ClassDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Internal_GetAttributesWithSingleParameter(attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Internal_GetAttributesWithSingleParameter(attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this PropertyDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Internal_GetAttributesWithSingleParameter(attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this MethodDeclarationSyntax syntaxNode, string attrName)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.Internal_GetAttributesWithSingleParameter(attrName);
		}

		public static bool HasBaseClass(this ClassDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.BaseList != null;
		}

		public static SeparatedSyntaxList<BaseTypeSyntax> GetBaseClasses(this ClassDeclarationSyntax syntaxNode)
		{
			ThrowAnErrorIfIsNull(syntaxNode);
			return syntaxNode.HasBaseClass() ? syntaxNode.BaseList.Types
				: (SeparatedSyntaxList<BaseTypeSyntax>)default;
		}

		public static IEnumerable<UsingDirectiveSyntax> GetAllUsingsDirective(this SyntaxNode node)
		{
			return GetDescendantDeclarationSyntax<UsingDirectiveSyntax>(node.SyntaxTree);
		}
	}
}