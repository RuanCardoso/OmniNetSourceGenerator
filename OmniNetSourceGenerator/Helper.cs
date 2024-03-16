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
						if (arguments[argumentIndex].Expression is T indexedArgument) // Check expression compatibility ex: 'Literal' with 'Member' = false, 'Literal' with 'Literal' = true
						{
							// Check type and order compatibility, ex, Literal with Literal, Ok, but the first is a 'byte' and the second is a 'int', not ok... byte with byte Ok, int with int, Ok.
							// Check if the expression is the same, if yes, Ok, if no, incorrect argument passed even if the two have the same type, this option will detect it.
							if (argument.Expression == indexedArgument)
								return indexedArgument;
							else continue;
						}
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
