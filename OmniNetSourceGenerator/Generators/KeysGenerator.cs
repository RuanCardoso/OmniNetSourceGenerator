using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Extensions;

/// <summary>
/// Generator responsible for creating random secret keys with randomized class and variable names for key obfuscation.
/// This generator helps protect sensitive information by generating obfuscated code that stores secret keys
/// in a way that makes them difficult to extract through static analysis or decompilation.
/// </summary>
/// <remarks>
/// The obfuscation technique uses random naming conventions and potentially splits the key across multiple
/// variables to increase the difficulty of identifying the actual secret key in the compiled code.
/// This provides an additional layer of security for applications that need to store encryption keys
/// or other sensitive data in the compiled assembly.
/// </remarks>
namespace OmniNetSourceGenerator
{
    [Generator]
    internal class KeysGenerator : ISourceGenerator
    {
        private static readonly Random _random = new Random();
        private readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private readonly string prefixes = "abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is KeysSyntaxReceiver receiver)
            {
                foreach (var @class in receiver.classes)
                {
                    string assemblyPath = GetNormalizedAssemblyPath(@class.SyntaxTree.FilePath);
                    string assemblyPathHash = GetAssemblyPathHash(assemblyPath);
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{@class.Identifier.Text}_keys_generated_code_{assemblyPathHash}.ini");

                    StringBuilder sbUsings = new StringBuilder();
                    sbUsings.AppendLine("#nullable disable");
                    sbUsings.AppendLine("#pragma warning disable");
                    sbUsings.AppendLine("using UnityEngine.Scripting;");
                    sbUsings.AppendLine($"// {assemblyPath} - Hash: {assemblyPathHash}");

                    StringBuilder sbClass = new StringBuilder();
                    ClassDeclarationSyntax keysClass = SyntaxFactory.ClassDeclaration(GenerateRandomName())
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                        );

                    for (int i = 0; i < 10; i++)
                        keysClass = keysClass.AddMembers(GenerateSecureRandomBytesField());

                    var fields = keysClass.Members.OfType<FieldDeclarationSyntax>().ToArray();
                    var randomField = fields[_random.Next(0, fields.Length)];
                    var fieldVariable = randomField.Declaration.Variables[0];

                    sbClass.AppendLine($"{@class.Modifiers} class {@class.Identifier.Text}");
                    sbClass.AppendLine("{");
                    sbClass.AppendLine($"    public const string __Internal__Key_Path__ = @\"{path}\";");
                    sbClass.AppendLine($"    internal static byte[] __Internal__Key__ => {keysClass.Identifier.Text}.{fieldVariable.Identifier.Text};");
                    sbClass.AppendLine("}");
                    sbClass.AppendLine();
                    sbClass.Append(keysClass.NormalizeWhitespace().ToString());

                    var @namespace = @class.GetNamespace(out bool hasNamespace);
                    if (hasNamespace)
                    {
                        sbUsings.AppendLine();
                        sbUsings.AppendLine($"namespace {@namespace.Name}");
                        sbUsings.AppendLine("{");
                        sbUsings.AppendLine("    " + sbClass.ToString().Replace("\n", "\n    ").TrimEnd());
                        sbUsings.AppendLine("}");
                    }
                    else
                    {
                        sbUsings.AppendLine();
                        sbUsings.AppendLine(sbClass.ToString());
                    }

                    string code = sbUsings.ToString();
                    bool exists = File.Exists(path);
                    if (!exists)
                    {
                        WriteKeysToFile(path, code);
                    }
                    else
                    {
                        string[] currentCode = File.ReadAllLines(path);
                        string date = currentCode[currentCode.Length - 1].Substring(3); // Skip the // and the space
                        if (DateTime.TryParse(date, out DateTime parsedDate))
                        {
                            if (DateTime.UtcNow.Subtract(parsedDate).TotalMinutes < 10000d) // 10000 minutes = 7 days, the keys are valid for 7 days
                            {
                                context.AddSource($"{@class.Identifier.Text}_keys_generated_code_.cs", string.Join("\n", currentCode));
                                return;
                            }

                            WriteKeysToFile(path, code);
                        }
                    }

                    context.AddSource($"{@class.Identifier.Text}_keys_generated_code_.cs", code);
                }
            }
        }

        private string GetNormalizedAssemblyPath(string assemblyPath)
        {
            return assemblyPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        private string GetAssemblyPathHash(string assemblyPath)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(assemblyPath));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new KeysSyntaxReceiver());
        }

        private string GenerateRandomName()
        {
            int length = _random.Next(4, 17); // 17 exclusive, so 4-16 inclusive
            char[] nameChars = new char[length];

            nameChars[0] = prefixes[_random.Next(prefixes.Length)];
            for (int i = 1; i < length; i++)
                nameChars[i] = chars[_random.Next(chars.Length)];

            return new string(nameChars);
        }

        private FieldDeclarationSyntax GenerateSecureRandomBytesField()
        {
            string fieldName = GenerateRandomName();
            int byteSize = _random.Next(16, 65); // 65 exclusive, so 16-64 inclusive

            byte[] secureBytes = new byte[byteSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(secureBytes);

            var arrayInitializer = SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                    secureBytes.Select(b => SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(b)
                    ))
                )
            );

            var arrayCreation = SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.ByteKeyword)
                    )
                ).WithRankSpecifiers(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()
                            )
                        )
                    )
                ),
                arrayInitializer
            );

            var variableDeclaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ArrayType(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.ByteKeyword)
                    )
                ).WithRankSpecifiers(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()
                            )
                        )
                    )
                )
            ).AddVariables(
                SyntaxFactory.VariableDeclarator(fieldName)
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(arrayCreation)
                    )
            );

            return SyntaxFactory.FieldDeclaration(variableDeclaration)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                ).AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Preserve"))
                        )
                    )
                );
        }

        private void WriteKeysToFile(string path, string content)
        {
            using (var file = File.Open(path, FileMode.OpenOrCreate))
            {
                using (StreamWriter writer = new StreamWriter(file))
                {
                    writer.WriteLine(content);
                    writer.WriteLine($"// {DateTime.UtcNow}");
                }
            }
        }
    }

    internal class KeysSyntaxReceiver : ISyntaxReceiver
    {
        internal List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.HasAttribute("GenerateSecureKeys"))
            {
                classes.Add(classDeclaration);
            }
        }
    }
}