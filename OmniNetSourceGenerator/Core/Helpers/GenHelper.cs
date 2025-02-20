using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGenerator.Extensions;

namespace SourceGenerator.Helpers
{
	public static class GenHelper
	{
		public static readonly DiagnosticDescriptor InvalidFieldNamingConventionIsUpper = new DiagnosticDescriptor(
			id: "OMNI003",
			title: "Invalid Field Name Capitalization",
			messageFormat: "The Field '{0}' with 'm_' prefix must follow PascalCase convention (e.g., 'm_PlayerHealth')",
			category: "Naming",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Fields using the 'm_' prefix must follow PascalCase naming convention where the first letter after 'm_' is uppercase. This helps maintain consistent code style and readability across the codebase."
		);

		public static readonly DiagnosticDescriptor InvalidFieldNamingConventionStartsWith = new DiagnosticDescriptor(
			id: "OMNI004",
			title: "Missing Required Field Prefix",
			messageFormat: "The Field '{0}' must start with 'm_' prefix and follow PascalCase convention",
			category: "Naming",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Member fields in this codebase must start with 'm_' prefix followed by PascalCase naming " +
				"(e.g., 'm_PlayerHealth', 'm_IsActive'). This convention helps distinguish member fields " +
				"from local variables and improves code readability."
		);

		public static readonly DiagnosticDescriptor PartialKeywordMissing = new DiagnosticDescriptor(
			id: "OMNI002",
			title: "Missing 'partial' Keyword in Source Generator Class",
			messageFormat: "The Class '{0}' must be declared with the 'partial' keyword to support source generation functionality",
			category: "Source Generation",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "When using source generators, target classes must be declared as 'partial' to allow the generator to add members. " +
				"This enables the compiler to combine the original class with the generated code."
		);

		public static readonly DiagnosticDescriptor InheritanceConstraintViolation = new DiagnosticDescriptor(
			id: "OMNI001",
			title: "Missing Network Inheritance",
			messageFormat: "The Class '{0}' must inherit from a network class (NetworkBehaviour, ServerBehaviour, or ClientBehaviour)",
			category: "Networking",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Classes using network functionality must inherit from one of the following base classes:\n" +
						"- NetworkBehaviour: For general network synchronization\n" +
						"- ServerBehaviour: For server-specific network logic\n" +
						"- ClientBehaviour: For client-specific network logic\n" +
						"This ensures proper network functionality and synchronization capabilities."
		);

		public static void ReportInheritanceRequirement(Context context, string className, Location location = null)
		{
			context.ReportDiagnostic(
				InheritanceConstraintViolation,
				location,
				className
			);
		}

		public static void ReportPartialKeywordRequirement(Context context, ClassDeclarationSyntax @class, Location location = null)
		{
			if (!@class.HasModifier(SyntaxKind.PartialKeyword))
			{
				string className = @class.Identifier.Text;
				context.ReportDiagnostic(
					PartialKeywordMissing,
					location,
					className
				);
			}
		}

		public static bool ReportInvalidFieldNamingIsUpper(Context context, string fieldName, Location location = null)
		{
			if (!char.IsUpper(fieldName[0]))
			{
				context.ReportDiagnostic(
					InvalidFieldNamingConventionIsUpper,
					location,
					fieldName
				);

				return true;
			}

			return false;
		}

		public static bool ReportInvalidFieldNamingStartsWith(Context context, string fieldName, Location location = null)
		{
			if (!fieldName.StartsWith("m_"))
			{
				context.ReportDiagnostic(
					InvalidFieldNamingConventionStartsWith,
					location,
					fieldName
				);

				return true;
			}

			return false;
		}

		public static bool ReportUnsupportedDualBehaviourUsage(GeneratorExecutionContext context, bool isDualBehaviour)
		{
			if (isDualBehaviour)
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

		/// <summary>
		/// Creates an empty statement.
		/// </summary>
		/// <returns>
		/// An instance of <see cref="StatementSyntax"/> representing an empty statement.
		/// </returns>
		public static StatementSyntax EmptyStatement()
		{
			return SyntaxFactory.ParseStatement("");
		}
	}

#if DEBUG
	internal static class Logger
	{
		private static readonly List<string> _logs = new List<string>();
		public static void Print(string msg) => _logs.Add("//\t" + msg);
		public static void Flush(GeneratorExecutionContext context)
		{
			context.AddSource($"logs.g.cs", SourceText.From(string.Join("\n", _logs), Encoding.UTF8));
		}
	}
#endif
}
