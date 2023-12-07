using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniNetSourceGenerator
{
	[Generator]
	public class RemoteCodeGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			RemoteSyntax remoteSyntax = (RemoteSyntax)context.SyntaxReceiver;
			if (remoteSyntax != null)
			{
				if (remoteSyntax.Methods.Count > 0)
				{
					foreach (MethodDeclarationSyntax methodDeclarationSyntax in remoteSyntax.Methods)
					{
						ClassDeclarationSyntax classDeclarationSyntax = GetClass(methodDeclarationSyntax);
						if (classDeclarationSyntax != null)
						{
							string methodName = methodDeclarationSyntax.Identifier.ToString();
							string className = classDeclarationSyntax.Identifier.ToString();
							if (!string.IsNullOrEmpty(methodName) && !string.IsNullOrEmpty(className))
							{
								StringBuilder builder = new StringBuilder();
								builder.AppendLine(GenerateInheritClass(className, methodName));
								builder.AppendLine($"public partial class {className} : {className}_g");
								builder.AppendLine("{");
								builder.AppendLine("}");
								context.AddSource($"{className}_g.cs", builder.ToString());
							}
						}
					}
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new RemoteSyntax());
		}

		private string GenerateInheritClass(string className, string methodName)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"public class {className}_g");
			builder.AppendLine("{");
			builder.AppendLine(GenerateServerMethod(methodName));
			builder.AppendLine(GenerateClientMethod(methodName));
			builder.AppendLine("}");
			return builder.ToString();
		}

		private string GenerateServerMethod(string methodName)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"public virtual void {methodName}ServerLogic(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)");
			builder.AppendLine("{");
			builder.AppendLine("}");
			return builder.ToString();
		}

		private string GenerateClientMethod(string methodName)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"public virtual void {methodName}ClientLogic(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)");
			builder.AppendLine("{");
			builder.AppendLine("}");
			return builder.ToString();
		}

		private ClassDeclarationSyntax GetClass(MethodDeclarationSyntax methodDeclarationSyntax)
		{
			return methodDeclarationSyntax.Ancestors().OfType<ClassDeclarationSyntax>().First();
		}
	}

	class RemoteSyntax : ISyntaxReceiver
	{
		public List<MethodDeclarationSyntax> Methods { get; private set; } = new List<MethodDeclarationSyntax>();
		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax)
			{
				if (methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					foreach (AttributeListSyntax attributeLists in methodDeclarationSyntax.AttributeLists)
					{
						foreach (AttributeSyntax attribute in attributeLists.Attributes)
						{
							if (attribute.Name is IdentifierNameSyntax identifierNameSyntax)
							{
								if (identifierNameSyntax.Identifier.Text == "Remote")
								{
									Methods.Add(methodDeclarationSyntax);
								}
							}
						}
					}
				}
			}
		}
	}
}
