using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace OmniNetSourceGenerator
{
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
		public static SemanticModel GetSemanticModel(this GeneratorExecutionContext context, SyntaxTree syntaxTree)
		{
			return context.Compilation.GetSemanticModel(syntaxTree);
		}

		public static bool GetDeclarationSyntax<T>(this SyntaxNode syntaxNode, out T declarationSyntax) where T : SyntaxNode
		{
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
			if (syntaxTree.TryGetRoot(out SyntaxNode root))
			{
				foreach (SyntaxNode syntaxNode in root.DescendantNodes().OfType<T>())
				{
					yield return syntaxNode as T;
				}
			}
		}

		public static IEnumerable<T> GetAncestorsDeclarationSyntax<T>(this SyntaxTree syntaxTree) where T : SyntaxNode
		{
			if (syntaxTree.TryGetRoot(out SyntaxNode root))
			{
				foreach (SyntaxNode syntaxNode in root.Ancestors().OfType<T>())
				{
					yield return syntaxNode as T;
				}
			}
		}

		public static IEnumerable<T> GetDescendantDeclarationSyntax<T>(this GeneratorExecutionContext context) where T : SyntaxNode
		{
			foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
			{
				if (syntaxTree.TryGetRoot(out SyntaxNode root))
				{
					foreach (SyntaxNode syntaxNode in root.DescendantNodes().OfType<T>())
					{
						yield return syntaxNode as T;
					}
				}
				else continue;
			}
		}

		public static IEnumerable<T> GetAncestorsDeclarationSyntax<T>(this GeneratorExecutionContext context) where T : SyntaxNode
		{
			foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
			{
				if (syntaxTree.TryGetRoot(out SyntaxNode root))
				{
					foreach (SyntaxNode syntaxNode in root.Ancestors().OfType<T>())
					{
						yield return syntaxNode as T;
					}
				}
				else continue;
			}
		}

		public static string GetIdentifierName(this PropertyDeclarationSyntax syntaxNode) => syntaxNode?.Identifier.Text;
		public static string GetIdentifierName(this MethodDeclarationSyntax syntaxNode) => syntaxNode?.Identifier.Text;
		public static string GetIdentifierName(this ClassDeclarationSyntax syntaxNode) => syntaxNode?.Identifier.Text;
		public static string GetIdentifierName(this NamespaceDeclarationSyntax syntaxNode) => syntaxNode?.Name.ToString();

		public static NamespaceDeclarationSyntax GetNamespaceDeclarationSyntax(this SyntaxNode syntaxNode)
		{
			return syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax
				? namespaceDeclarationSyntax
				: syntaxNode.Parent is null ? null : GetNamespaceDeclarationSyntax(syntaxNode.Parent);
		}

		public static string GetNamespaceName(this SyntaxNode syntaxNode)
		{
			return GetNamespaceDeclarationSyntax(syntaxNode)?.GetIdentifierName();
		}

		public static ClassDeclarationSyntax GetClassDeclarationSyntax(this SyntaxNode syntaxNode)
		{
			return syntaxNode.Parent is ClassDeclarationSyntax classDeclarationSyntax
				? classDeclarationSyntax
				: syntaxNode.Parent is null ? null : GetClassDeclarationSyntax(syntaxNode.Parent);
		}

		public static string GetClassName(this SyntaxNode syntaxNode)
		{
			return GetClassDeclarationSyntax(syntaxNode)?.GetIdentifierName();
		}

		public static bool HasModifier(this MemberDeclarationSyntax syntaxNode, SyntaxKind syntaxKind) => syntaxNode.Modifiers.Any(syntaxKind);
		public static bool HasModifier(this PropertyDeclarationSyntax syntaxNode, SyntaxKind syntaxKind) => syntaxNode.Modifiers.Any(syntaxKind);
		public static bool HasModifier(this ClassDeclarationSyntax syntaxNode, SyntaxKind syntaxKind) => syntaxNode.Modifiers.Any(syntaxKind);
		public static bool HasModifier(this MethodDeclarationSyntax syntaxNode, SyntaxKind syntaxKind) => syntaxNode.Modifiers.Any(syntaxKind);
		public static bool HasModifier(this NamespaceDeclarationSyntax syntaxNode, SyntaxKind syntaxKind) => syntaxNode.Modifiers.Any(syntaxKind);

		private static bool Internal_HasAttribute(MemberDeclarationSyntax syntaxNode, string attrName)
		{
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
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this ClassDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		public static bool HasAttribute(this PropertyDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_HasAttribute(syntaxNode, attrName);
		}

		private static IEnumerable<AttributeWithMultipleParameters> Internal_GetAttributesWithMultipleParameters(this MemberDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
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
								string pTypeName = semanticModel.GetTypeInfo(attributeArgumentSyntax.Expression).ConvertedType.Name;
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
			return Internal_GetAttributesWithMultipleParameters(syntaxNode, semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this PropertyDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			return Internal_GetAttributesWithMultipleParameters(syntaxNode, semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this MemberDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			return Internal_GetAttributesWithMultipleParameters(syntaxNode, semanticModel, attrName);
		}

		public static IEnumerable<AttributeWithMultipleParameters> GetAttributesWithMultipleParameters(this MethodDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			return Internal_GetAttributesWithMultipleParameters(syntaxNode, semanticModel, attrName);
		}

		private static IEnumerable<string> Internal_GetAttributesWithSingleParameter(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
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

		public static IEnumerable<string> GetAttributesWithSingleParameter(this ClassDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_GetAttributesWithSingleParameter(syntaxNode, attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this MemberDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_GetAttributesWithSingleParameter(syntaxNode, attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this PropertyDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_GetAttributesWithSingleParameter(syntaxNode, attrName);
		}

		public static IEnumerable<string> GetAttributesWithSingleParameter(this MethodDeclarationSyntax syntaxNode, string attrName)
		{
			return Internal_GetAttributesWithSingleParameter(syntaxNode, attrName);
		}

		public static bool HasBaseClass(this ClassDeclarationSyntax syntaxNode)
		{
			return syntaxNode.BaseList != null;
		}
	}
}