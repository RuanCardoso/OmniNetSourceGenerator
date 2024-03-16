using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace OmniNetSourceGenerator
{
	internal static class Helper
	{
		public static T GetArgumentExpression<T>(string argumentName, int argumentIndex, SeparatedSyntaxList<AttributeArgumentSyntax> arguments) where T : ExpressionSyntax
		{
			foreach (AttributeArgumentSyntax argument in arguments)
			{
				if (argument.NameColon != null)
				{
					if (IsIdentifierName(argument.NameColon.Name, argumentName))
					{
						return (T)argument.Expression;
					}
					else continue;
				}
				else if (argument.NameEquals != null)
				{
					if (IsIdentifierName(argument.NameEquals.Name, argumentName))
					{
						return (T)argument.Expression;
					}
					else continue;
				}
				else
				{
					if (arguments.Count <= argumentIndex)
						continue;

					if (arguments[argumentIndex] != null)
					{
						T indexedArgument = (T)arguments[argumentIndex].Expression;
						if (argument.Expression == indexedArgument)
							return indexedArgument;
						else continue;
					}
					else continue;
				}
			}

			return null;
		}

		private static bool IsIdentifierName(IdentifierNameSyntax identifier, string argumentName)
		{
			return identifier.Identifier.Text.ToLowerInvariant() == argumentName.ToLowerInvariant();
		}
	}
}
