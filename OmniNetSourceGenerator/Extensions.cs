using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace OmniNetSourceGenerator
{
	internal static class Extensions
	{
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

		public static SemanticModel GetSemanticModel(this GeneratorExecutionContext context, SyntaxTree syntaxTree)
		{
			return context.Compilation.GetSemanticModel(syntaxTree);
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

		public static string GetIdentifierName(this MethodDeclarationSyntax syntaxNode)
		{
			return syntaxNode.Identifier.Text;
		}

		public static string GetIdentifierName(this ClassDeclarationSyntax syntaxNode)
		{
			return syntaxNode.Identifier.Text;
		}

		public static string GetNamespaceName(this SyntaxNode syntaxNode)
		{
			return syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax
				? namespaceDeclarationSyntax.Name.ToString()
				: syntaxNode.Parent is null ? string.Empty : GetNamespaceName(syntaxNode.Parent);
		}

		public static bool HasModifier(this MemberDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasModifier(this PropertyDeclarationSyntax syntaxNode, SyntaxKind syntaxKind)
		{
			return syntaxNode.Modifiers.Any(syntaxKind);
		}

		public static bool HasAttribute(this MemberDeclarationSyntax syntaxNode, string attrName)
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

		public static bool HasAttribute(this PropertyDeclarationSyntax syntaxNode, string attrName)
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

		public static IEnumerable<(string name, string value, string type)> GetAttributeParametersWithInfo(this MemberDeclarationSyntax syntaxNode, SemanticModel semanticModel, string attrName)
		{
			foreach (AttributeListSyntax listSyntax in syntaxNode.AttributeLists)
			{
				foreach (var attributeSyntax in listSyntax.Attributes)
				{
					if (attributeSyntax.Name is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.Text == attrName)
					{
						if (attributeSyntax.ArgumentList != null)
						{
							int index = 0;
							foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
							{
								string pName = attributeArgumentSyntax.NameColon?.ToString() ?? $"p{index++}";
								string pTypeName = semanticModel.GetTypeInfo(attributeArgumentSyntax.Expression).ConvertedType.Name;
								yield return attributeArgumentSyntax.Expression is LiteralExpressionSyntax literalExpressionSyntax
									? ((string pName, string pValue, string pType))(pName, literalExpressionSyntax.Token.ValueText, pTypeName)
									: ((string pName, string pValue, string pType))(pName, attributeArgumentSyntax.Expression.ToString(), pTypeName);
							}
						}
						else continue;
					}
					else continue;
				}
			}
		}

		public static IEnumerable<string> GetAttributeParameters(this MemberDeclarationSyntax syntaxNode, string attrName)
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

		public static IEnumerable<string> GetAttributeParameters(this PropertyDeclarationSyntax syntaxNode, string attrName)
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

		public static bool HasBaseClass(this ClassDeclarationSyntax syntaxNode)
		{
			return syntaxNode.BaseList != null;
		}
	}
}