using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Helpers;

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
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "__omni_development_keys__"
            );

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#nullable disable");
            sb.AppendLine("#pragma warning disable");
            sb.AppendLine("using UnityEngine.Scripting;");

            ClassDeclarationSyntax parentClass = SyntaxFactory.ClassDeclaration(GenerateRandomName())
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                );

            for (int i = 0; i < 5; i++)
                parentClass = parentClass.AddMembers(GenerateSecureRandomBytesField());

            var fields = parentClass.Members.OfType<FieldDeclarationSyntax>().ToArray();
            var randomField = fields[_random.Next(fields.Length)];
            var fieldVariable = randomField.Declaration.Variables[0];

            sb.AppendLine("internal static class __O_Keys__");
            sb.AppendLine("{");
            sb.AppendLine($"    internal static byte[] __Internal__Key__ => {parentClass.Identifier.Text}.{fieldVariable.Identifier.Text};");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.Append(parentClass.NormalizeWhitespace().ToString());
            string content = sb.ToString();

            bool exists = File.Exists(path);
            if (!exists)
                WriteKeysToFile(path, content);

            if (exists)
            {
                string[] oldCode = File.ReadAllLines(path);
                string date = oldCode[oldCode.Length - 1].Substring(3); // Skip the // and the space
                if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    if (DateTime.UtcNow.Subtract(parsedDate).TotalMinutes < 10000d) // 10000 minutes = 7 days, the keys are valid for 7 days
                    {
                        context.AddSource($"_keys_generated_code_.cs", string.Join("\n", oldCode));
                        return;
                    }

                    WriteKeysToFile(path, content);
                }
            }

            context.AddSource($"_keys_generated_code_.cs", sb.ToString());
        }

        public void Initialize(GeneratorInitializationContext context) { }

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
}