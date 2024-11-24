using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator.Extensions
{
	public class ClassStructure
	{
		public ClassDeclarationSyntax ParentClass { get; }
		public IEnumerable<MemberDeclarationSyntax> Members { get; }

		public ClassStructure(ClassDeclarationSyntax parentClass, IEnumerable<MemberDeclarationSyntax> members)
		{
			ParentClass = parentClass;
			Members = members;
		}
	}

	public static class GenExtensions
	{
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
			return member.AttributeLists
			 .SelectMany(x =>
				  x.Attributes.Where(y =>
					  y.ArgumentList != null &&
					  y.ArgumentList.Arguments.Count > 0 && y.Name.ToString() == attributeName
				  )
			 );
		}

		public static IEnumerable<AttributeSyntax> GetAttributes(this MemberDeclarationSyntax member, params string[] attributeNames)
		{
			return attributeNames.SelectMany(x => member.GetAttributes(x));
		}

		public static IEnumerable<ClassStructure> GroupMembersByParentClass(this IEnumerable<MemberDeclarationSyntax> members)
		{
			return members.GroupBy(member => ((ClassDeclarationSyntax)member.Parent)).Select(group => new ClassStructure(group.Key, group));
		}

		public static IEnumerable<T> GetDescendantsOfType<T>(this SyntaxNode node) where T : SyntaxNode
		{
			return node?.DescendantNodes().OfType<T>() ?? Enumerable.Empty<T>();
		}

		public static bool HasModifier(this MemberDeclarationSyntax member, SyntaxKind modifier)
		{
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

		public static bool HasBaseType(this ClassDeclarationSyntax @class, string baseType)
		{
			return @class.BaseList != null && @class.BaseList.Types.Any(x => x.ToString().Contains(baseType));
		}

		public static bool HasBaseType(this ClassDeclarationSyntax @class, params string[] baseType)
		{
			return baseType.Any(x => @class.HasBaseType(x));
		}

		public static string GetBaseTypeName(this ClassDeclarationSyntax @class)
		{
			if (@class.BaseList == null)
			{
				return string.Empty;
			}

			if (@class.BaseList.Types.Count <= 0)
			{
				return string.Empty;
			}

			return @class.BaseList.Types[0].ToString();
		}
	}
}