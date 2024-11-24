using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator.Helpers
{
	public static class GenHelper
	{
		public static void ReportInheritanceRequirement(GeneratorExecutionContext context)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					new DiagnosticDescriptor(
						"OMNI001",
						"Class Inheritance Constraint Violation",
						"The class must inherit from any `NetworkEventBase` or `NetworkBehaviour` to ensure proper network functionality.",
						"Design",
						DiagnosticSeverity.Error,
						isEnabledByDefault: true
					),
					Location.None
				)
			);
		}

		public static void ReportPartialKeywordRequirement(GeneratorExecutionContext context)
		{
			context.ReportDiagnostic(
				Diagnostic.Create(
					new DiagnosticDescriptor(
						"OMNI002",
						"Partial Keyword Missing",
						"The class definition must include the 'partial' keyword to enable proper functionality in this context.",
						"Design",
						DiagnosticSeverity.Error,
						isEnabledByDefault: true
					),
					Location.None
				)
			);
		}

		public static bool ReportInvalidFieldNamingConvention(GeneratorExecutionContext context, string fieldName)
		{
			if (!char.IsUpper(fieldName[0]))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						new DiagnosticDescriptor(
							"OMNI003",
							"Invalid Field Naming Convention",
							"The field name prefixed with 'm_' must have its first letter capitalized, such as 'm_Power'. Please correct the naming convention.",
							"Design",
							DiagnosticSeverity.Error,
							isEnabledByDefault: true
						),
						Location.None
					)
				);

				return true;
			}

			return false;
		}

		public static bool ReportInvalidFieldNaming(GeneratorExecutionContext context, string fieldName)
		{
			if (!fieldName.StartsWith("m_"))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						new DiagnosticDescriptor(
							"OMNI004",
							"Invalid Field Naming Convention",
							$"The field '{fieldName}' does not follow the required naming convention. It should start with 'm_' and be in PascalCase, such as 'm_FieldName'. Please update the field name to comply with this convention.",
							"Design",
							DiagnosticSeverity.Error,
							isEnabledByDefault: true
						),
						Location.None
					)
				);

				return true;
			}

			return false;
		}

		public static bool ReportUnsupportedDualBehaviourUsage(GeneratorExecutionContext context, string baseClassName)
		{
			if (baseClassName == "DualBehaviour")
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						new DiagnosticDescriptor(
							"OMNI005",
							"Unsupported Usage of DualBehaviour",
							"The 'DualBehaviour' class is supported only with manually created properties. Auto-generated properties are not supported. Please ensure 'DualBehaviour' is used with manually created properties.",
							"Design",
							DiagnosticSeverity.Error,
							isEnabledByDefault: true
						),
						Location.None
					)
				);

				return true;
			}

			return false;
		}


		public static T GetArgumentExpression<T>(string argumentName, int argumentIndex, SeparatedSyntaxList<AttributeArgumentSyntax> arguments) where T : ExpressionSyntax
		{
			bool IsIdentifierName(IdentifierNameSyntax identifier)
			{
				return identifier.Identifier.Text.ToLowerInvariant() == argumentName.ToLowerInvariant();
			}

			foreach (AttributeArgumentSyntax argument in arguments)
			{
				if (argument.NameColon != null)
				{
					if (IsIdentifierName(argument.NameColon.Name))
					{
						return (T)argument.Expression;
					}
					else continue;
				}
				else if (argument.NameEquals != null)
				{
					if (IsIdentifierName(argument.NameEquals.Name))
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
	}
}
