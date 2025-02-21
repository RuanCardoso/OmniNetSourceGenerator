using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Extensions
{
	public static class GenExtensions
	{
		public static bool HasAttributeOfType(this ITypeSymbol symbol, string baseAttributeName)
		{
			return symbol.GetAttributes().Any(x =>
				x.AttributeClass?.InheritsFromClass(baseAttributeName) == true);
		}

		public static bool HasAttributeOfType(this MemberDeclarationSyntax member,
			string baseAttributeName,
			SemanticModel semanticModel)
		{
			return member.AttributeLists.Any(x => x.Attributes.Any(y =>
			{
				var typeInfo = semanticModel.GetTypeInfo(y);
				return typeInfo.Type?.InheritsFromClass(baseAttributeName) == true;
			}));
		}

		public static bool HasAttributeOfType(this MemberDeclarationSyntax member,
			SemanticModel semanticModel,
			params string[] baseAttributeNames)
		{
			return baseAttributeNames.Any(x => member.HasAttributeOfType(x, semanticModel));
		}

		public static IEnumerable<AttributeSyntax> GetAttributesOfType(
			this MemberDeclarationSyntax member,
			string baseAttributeName,
			SemanticModel semanticModel)
		{
			return member.AttributeLists.SelectMany(x => x.Attributes.Where(y =>
			{
				var typeInfo = semanticModel.GetTypeInfo(y);
				return typeInfo.Type?.InheritsFromClass(baseAttributeName) == true;
			}));
		}

		public static AttributeSyntax GetAttributeOfType(
			this MemberDeclarationSyntax member,
			string baseAttributeName,
			SemanticModel semanticModel)
		{
			return GetAttributesOfType(member, baseAttributeName, semanticModel).FirstOrDefault();
		}

		public static IEnumerable<AttributeSyntax> GetAttributesOfType(
			this MemberDeclarationSyntax member,
			SemanticModel semanticModel,
			params string[] baseAttributeNames)
		{
			return baseAttributeNames.SelectMany(x =>
				member.GetAttributesOfType(x, semanticModel));
		}

		public static bool HasAttribute(this ITypeSymbol symbol, string attributeName)
		{
			return symbol.GetAttributes().Any(x => x.AttributeClass?.Name == attributeName || x.AttributeClass?.ToDisplayString() == attributeName);
		}

		public static bool HasAttribute(this MemberDeclarationSyntax member, string attributeName)
		{
			return member.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == attributeName));
		}

		public static bool HasAttribute(this MemberDeclarationSyntax member, params string[] attributeNames)
		{
			return attributeNames.Any(x => member.HasAttribute(x));
		}

		public static IEnumerable<AttributeSyntax> GetAttributes(this MemberDeclarationSyntax member, string attributeName)
		{
			return member.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == attributeName));
		}

		public static AttributeSyntax GetAttribute(this MemberDeclarationSyntax member, string attributeName)
		{
			return member.GetAttributes(attributeName).FirstOrDefault();
		}

		public static IEnumerable<AttributeSyntax> GetAttributes(this MemberDeclarationSyntax member, params string[] attributeNames)
		{
			return attributeNames.SelectMany(x => member.GetAttributes(x));
		}

		public static IEnumerable<ClassStructure> GroupByDeclaringClass(this IEnumerable<MemberDeclarationSyntax> members)
		{
			return members
				.GroupBy(
					member => ((ClassDeclarationSyntax)member.Parent).Identifier.Text,
					member => member,
					(className, groupMembers) =>
					{
						var firstClass = (ClassDeclarationSyntax)groupMembers.First().Parent;
						return new ClassStructure(firstClass, groupMembers);
					});
		}

		public static IEnumerable<T> GetDescendantsOfType<T>(this SyntaxNode node) where T : SyntaxNode
		{
			return node?.DescendantNodes().OfType<T>() ?? Enumerable.Empty<T>();
		}

		public static bool HasModifier(this MemberDeclarationSyntax member, SyntaxKind modifier)
		{
			if (modifier == SyntaxKind.PrivateKeyword)
			{
				if (member.Modifiers != null && member.Modifiers.Count == 0)
				{
					return true;
				}
			}

			return member.Modifiers.Any(modifier);
		}

		public static NamespaceDeclarationSyntax GetNamespace(this ClassDeclarationSyntax @class, out bool hasNamespace)
		{
			hasNamespace = false;
			if (@class.Parent is NamespaceDeclarationSyntax @namespace)
			{
				hasNamespace = true;
				return @namespace;
			}

			return default;
		}

		public static ClassDeclarationSyntax Clear(this ClassDeclarationSyntax @class, out ClassDeclarationSyntax fromClass)
		{
			fromClass = @class;
			return @class.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>());
		}

		public static NamespaceDeclarationSyntax Clear(this NamespaceDeclarationSyntax @namespace, out NamespaceDeclarationSyntax fromNamespace)
		{
			fromNamespace = @namespace;
			return @namespace.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>());
		}

		public static int GetBaseDepth(this ClassDeclarationSyntax @class, SemanticModel semanticModel, params string[] rootBases)
		{
			if (!(semanticModel.GetDeclaredSymbol(@class) is INamedTypeSymbol classSymbol))
			{
				return -1;
			}

			int depth = 0;
			INamedTypeSymbol currentBase = classSymbol.BaseType;
			while (currentBase != null)
			{
				++depth;
				if (rootBases.Any(x => x == currentBase.Name || x == currentBase.ToDisplayString()))
				{
					return depth;
				}

				currentBase = currentBase.BaseType;
			}

			return depth;
		}

		public static bool InheritsFromClass(this ITypeSymbol symbol, string baseName)
		{
			INamedTypeSymbol currentBase = symbol.BaseType;
			while (currentBase != null)
			{
				if (currentBase.Name == baseName || currentBase.ToDisplayString() == baseName)
					return true;

				currentBase = currentBase.BaseType;
			}

			return false;
		}

		public static bool InheritsFromClass(this ClassDeclarationSyntax @class, SemanticModel semanticModel, string baseName)
		{
			if (!(semanticModel.GetDeclaredSymbol(@class) is INamedTypeSymbol classNamedSymbol))
			{
				return false;
			}

			return classNamedSymbol.InheritsFromClass(baseName);
		}

		public static bool InheritsFromInterface(this ITypeSymbol symbol, string interfaceName)
		{
			if (symbol == null)
				return false;

			return symbol.AllInterfaces.Any(x => x.Name == interfaceName || x.ToDisplayString() == interfaceName);
		}

		public static bool InheritsFromInterface(this TypeSyntax type, SemanticModel semanticModel, string interfaceName)
		{
			ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(type).Type;
			return typeSymbol.InheritsFromInterface(interfaceName);
		}

		public static bool InheritsFromInterface(this TypeSyntax type, SemanticModel semanticModel, params string[] interfaceNames)
		{
			return interfaceNames.Any(x => type.InheritsFromInterface(semanticModel, x));
		}

		public static bool InheritsFromClass(this ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, params string[] baseNames)
		{
			return baseNames.Any(x => classDeclaration.InheritsFromClass(semanticModel, x));
		}

		public static bool InheritsFromClass(this ClassDeclarationSyntax @class, string baseName)
		{
			return @class.BaseList != null && @class.BaseList.Types.Any(x => x.ToString().Contains(baseName));
		}

		public static bool InheritsFromClass(this ClassDeclarationSyntax @class, params string[] baseNames)
		{
			return baseNames.Any(x => @class.InheritsFromClass(x));
		}

		public static T GetArgumentExpression<T>(this AttributeSyntax attribute, string argumentName, ArgumentIndex index) where T : ExpressionSyntax
		{
			int i = (int)index;
			return attribute.GetArgumentExpression<T>(argumentName, i);
		}

		/// <summary>
		/// Retrieves an argument expression from the specified attribute and returns it cast to the specified type <typeparamref name="T"/>.
		/// The search is performed by first matching a named argument (<paramref name="argumentName"/>) and, if not found,
		/// by using the argument at the specified <paramref name="argumentIndex"/>.
		/// </summary>
		/// <typeparam name="T">The type of the expression to retrieve, which must be a subclass of <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax"/>.</typeparam>
		/// <param name="attribute">The attribute syntax from which to extract the argument expression.</param>
		/// <param name="argumentName">The name of the argument to look for in the attribute's argument list.</param>
		/// <param name="argumentIndex">The index of the argument to fetch if a named argument match is not found.</param>
		/// <returns>
		/// The argument expression cast to type <typeparamref name="T"/>, or <c>null</c> if the argument is not found,
		/// not of the expected type, or if an exception occurs during the extraction process.
		/// </returns>
		public static T GetArgumentExpression<T>(this AttributeSyntax attribute, string argumentName, int argumentIndex) where T : ExpressionSyntax
		{
			try
			{
				if (attribute.ArgumentList == null)
					return null;

				var arguments = attribute.ArgumentList.Arguments.ToArray();
				if (arguments.Length == 0)
					return null;

				foreach (var argument in arguments)
				{
					if (argument.NameColon?.Name is IdentifierNameSyntax nameColon &&
						string.Equals(nameColon.Identifier.Text, argumentName, StringComparison.OrdinalIgnoreCase))
					{
						return argument.Expression as T;
					}

					if (argument.NameEquals?.Name is IdentifierNameSyntax nameEquals &&
						string.Equals(nameEquals.Identifier.Text, argumentName, StringComparison.OrdinalIgnoreCase))
					{
						return argument.Expression as T;
					}
				}

				if (argumentIndex >= 0 && argumentIndex < arguments.Length)
				{
					var indexedArgument = arguments[argumentIndex].Expression;
					if (indexedArgument is T typedArgument)
					{
						return typedArgument;
					}
				}

				return null;
			}
			catch
			{
				return null;
			}
		}
	}
}