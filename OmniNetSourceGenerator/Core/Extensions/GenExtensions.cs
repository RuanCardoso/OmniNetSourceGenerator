using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SourceGenerator.Extensions
{
	public static class GenExtensions
	{
		public static bool IsDelegate(this ITypeSymbol symbol)
		{
			if (symbol is INamedTypeSymbol namedTypeSymbol)
			{
				return namedTypeSymbol.TypeKind == TypeKind.Delegate;
			}

			return false;
		}

		public static ImmutableArray<ITypeSymbol> GetDelegateParameters(this ITypeSymbol symbol)
		{
			if (symbol is INamedTypeSymbol namedTypeSymbol)
			{
				if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
				{
					if (namedTypeSymbol.IsGenericType)
					{
						return namedTypeSymbol.TypeArguments;
					}
				}
			}

			return ImmutableArray<ITypeSymbol>.Empty;
		}

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

		public static NamespaceDeclarationSyntax GetNamespace(this StructDeclarationSyntax @struct, out bool hasNamespace)
		{
			hasNamespace = false;
			if (@struct.Parent is NamespaceDeclarationSyntax @namespace)
			{
				hasNamespace = true;
				return @namespace;
			}

			return default;
		}

		public static ClassDeclarationSyntax Clear(this ClassDeclarationSyntax @class, out ClassDeclarationSyntax fromClass)
		{
			fromClass = @class;
			return @class.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>()).
				WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>()).
				WithoutLeadingTrivia().
				WithoutTrailingTrivia();
		}

		public static StructDeclarationSyntax Clear(this StructDeclarationSyntax @struct, out StructDeclarationSyntax fromStruct)
		{
			fromStruct = @struct;
			return @struct.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>()).
				WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>()).
				WithoutLeadingTrivia().
				WithoutTrailingTrivia();
		}

		public static NamespaceDeclarationSyntax Clear(this NamespaceDeclarationSyntax @namespace, out NamespaceDeclarationSyntax fromNamespace)
		{
			fromNamespace = @namespace;
			return @namespace.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>()).
				WithoutLeadingTrivia().
				WithoutTrailingTrivia();
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

		public static T GetArgumentValue<T>(this AttributeSyntax attribute, string argumentName, ArgumentIndex index, SemanticModel semanticModel, T defaultValue = default)
		{
			int i = (int)index;
			return attribute.GetArgumentValue<T>(argumentName, i, semanticModel, defaultValue);
		}

		public static TResult GetArgumentValue<TResult>(this AttributeSyntax attribute, string argumentName, int argumentIndex, SemanticModel semanticModel, TResult defaultValue = default)
		{
			try
			{
				if (attribute.ArgumentList == null)
					return defaultValue;

				var arguments = attribute.ArgumentList.Arguments.ToArray();
				if (arguments.Length == 0)
					return defaultValue;

				// Parametros opcionais
				foreach (var argument in arguments)
				{
					string argName = null;

					if (argument.NameColon?.Name is IdentifierNameSyntax nameColon)
						argName = nameColon.Identifier.Text;

					if (argument.NameEquals?.Name is IdentifierNameSyntax nameEquals)
						argName = nameEquals.Identifier.Text;

					if (argName != null &&
						string.Equals(argName, argumentName, StringComparison.OrdinalIgnoreCase))
					{
						var expr = argument.Expression;
						var constValue = semanticModel.GetConstantValue(expr);
						if (constValue.HasValue)
						{
							if (typeof(TResult).IsEnum)
							{
								try { return (TResult)Enum.ToObject(typeof(TResult), constValue.Value); }
								catch { return defaultValue; }
							}

							if (constValue.Value is TResult val)
								return val;

							if (constValue.Value is IConvertible convertible)
							{
								try { return (TResult)Convert.ChangeType(convertible, typeof(TResult)); }
								catch { return defaultValue; }
							}
						}

						return defaultValue;
					}
				}

				// Parametros obrigatorios
				if (argumentIndex >= 0 && argumentIndex < arguments.Length)
				{
					var symbolInfo = semanticModel.GetSymbolInfo(attribute);
					if (symbolInfo.Symbol is IMethodSymbol ctorSymbol)
					{
						if (argumentIndex < ctorSymbol.Parameters.Length)
						{
							var param = ctorSymbol.Parameters[argumentIndex];
							if (string.Equals(param.Name, argumentName, StringComparison.OrdinalIgnoreCase))
							{
								var expr = arguments[argumentIndex].Expression;
								var constValue = semanticModel.GetConstantValue(expr);
								if (constValue.HasValue)
								{
									if (constValue.Value is TResult val)
										return val;

									if (constValue.Value is IConvertible convertible)
									{
										try { return (TResult)Convert.ChangeType(convertible, typeof(TResult)); }
										catch { return defaultValue; }
									}
								}
							}
						}
					}
				}

				return defaultValue;
			}
			catch
			{
				return defaultValue;
			}
		}
	}
}