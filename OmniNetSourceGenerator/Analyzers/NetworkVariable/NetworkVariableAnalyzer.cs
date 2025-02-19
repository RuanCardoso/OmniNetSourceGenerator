using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGenerator.Extensions;
using SourceGenerator.Helpers;

namespace OmniNetSourceGenerator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NetworkVariableAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor NetworkVariableFieldShouldBePrivate = new DiagnosticDescriptor(
            id: "OMNI018",
            title: "Network Variable Field Should Be Private",
            messageFormat: "The Network Variable '{0}' should be private as it is exposed through a generated public property",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Network variable fields should be declared as private since they are automatically exposed " +
                        "through generated public properties. This follows encapsulation best practices and avoids " +
                        "having two public access points to the same data and best Unity Inspector integration."
        );

        public static readonly DiagnosticDescriptor NonValueTypeEqualityCheck = new DiagnosticDescriptor(
            id: "OMNI020",
            title: "Performance: Reference Type Equality Check",
            messageFormat: "The Network Variable '{0}' has 'CheckEquality' enabled which may impact performance for reference types. Disable it if not needed.",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor HighBandwidthUsageWarning = new DiagnosticDescriptor(
            id: "OMNI021",
            title: "Performance: High Bandwidth Usage",
            messageFormat: "The Network Variable '{0}' has 'CheckEquality' disabled for a value type which may lead to unnecessary network traffic",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Value types should typically use equality checking to reduce network traffic. " +
                    "Disabling equality checks means the value will be synchronized even when unchanged."
        );

        public static readonly DiagnosticDescriptor ClientAuthorityWarning = new DiagnosticDescriptor(
            id: "OMNI022",
            title: "Security: Client Authority Risk Detection",
            messageFormat: "The Network Variable '{0}' has client authority enabled which poses potential security risks",
            category: "Security",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Network variables with client authority enabled can lead to several security vulnerabilities:" +
                        "\n1. Cheating: Clients can manipulate values directly without server validation" +
                        "\n2. Game State Exploitation: Malicious clients could corrupt game state" +
                        "\n3. Denial of Service: Rapid value changes could flood the network" +
                        "\n\nRecommended actions:" +
                        "\n- Implement server-side validation for all client authority changes" +
                        "\n- Use rate limiting for value updates" +
                        "\n- Consider if server authority would be more appropriate" +
                        "\n- Add sanity checks for value ranges" +
                        "\n- Log suspicious update patterns"
        );

        public static readonly DiagnosticDescriptor RequiresOwnershipWarning = new DiagnosticDescriptor(
            id: "OMNI023",
            title: "Security: Ownership Validation Required",
            messageFormat: "The Network Variable '{0}' has ownership validation disabled which poses security risks",
            category: "Security",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Network variables without ownership validation can lead to security vulnerabilities:" +
                        "\n1. Unauthorized Access: Any client could modify the variable without proving ownership" +
                        "\n2. Race Conditions: Multiple clients could attempt modifications simultaneously" +
                        "\n3. State Inconsistency: Can lead to desynchronization between clients" +
                        "\n4. Authority Bypass: Bypasses the normal authority system checks" +
                        "\n\nRecommended actions:" +
                        "\n- Enable RequiresOwnership for sensitive network variables" +
                        "\n- Implement proper ownership transfer mechanisms" +
                        "\n- Add server-side validation for ownership claims" +
                        "\n- Consider using authority-based permissions instead" +
                        "\n- Monitor and log unauthorized modification attempts"
        );

        public static readonly DiagnosticDescriptor ClientAuthorityWithoutOwnershipWarning = new DiagnosticDescriptor(
            id: "OMNI024",
            title: "Critical Security: Dangerous Authority Configuration",
            messageFormat: "The Network Variable '{0}' has a high-risk configuration: Client Authority is enabled while Ownership validation is disabled. " +
                          "This combination allows any client to modify the variable without ownership verification, " +
                          "potentially leading to severe security vulnerabilities and exploitation.",
            category: "Security",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "This combination of settings creates several critical security risks:" +
                        "\n1. Any client can modify the variable without proving ownership" +
                        "\n2. No built-in protection against malicious modifications" +
                        "\n3. High vulnerability to cheating and exploitation" +
                        "\n4. Potential for network flooding attacks" +
                        "\n\nImmediate actions required:" +
                        "\n- Enable ownership validation (RequiresOwnership = true)" +
                        "\n- Implement strict server-side validation" +
                        "\n- Add rate limiting for modifications" +
                        "\n- Consider redesigning the network architecture" +
                        "\n- Implement comprehensive security logging"
        );

        public static readonly DiagnosticDescriptor CollectionTypeWarning = new DiagnosticDescriptor(
            id: "OMNI025",
            title: "Design: Non-Observable Collection Usage",
            messageFormat: "The Network Variable '{0}' uses {1}. Consider using {2} for better network synchronization and change tracking",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Using standard collections in network variables can lead to synchronization issues:" +
                        "\n1. Changes to the collection aren't automatically detected" +
                        "\n2. Full collection resynchronization may be required" +
                        "\n3. Higher bandwidth usage due to inefficient syncing" +
                        "\n\nRecommended actions:" +
                        "\n- Use ObservableList instead of List<T>" +
                        "\n- Use ObservableDictionary instead of Dictionary<K,V>" +
                        "\n- Consider using custom Observable collections for other types" +
                        "\n- Implement proper change tracking mechanisms"
        );

        public static readonly DiagnosticDescriptor BooleanCountWarning = new DiagnosticDescriptor(
            id: "OMNI026",
            title: "Performance: Multiple Boolean Fields",
            messageFormat: "The Class '{0}' contains {1} boolean fields. Consider using a custom serializer to pack them into a single byte for better bandwidth efficiency.",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Multiple boolean fields in a network variable can lead to bandwidth inefficiency:" +
                        "\n1. Each boolean typically uses a full byte" +
                        "\n2. 8 booleans could be packed into a single byte" +
                        "\n3. Increased network traffic for synchronized data" +
                        "\n\nRecommended actions:" +
                        "\n- Implement a custom serializer to pack booleans" +
                        "\n- Use DataBuffer or similar bit-packing structure" +
                        "\n- Consider using flags enum for multiple states" +
                        "\n- Group related boolean fields into a custom structure"
        );

        public static readonly DiagnosticDescriptor MonoBehaviourSerializationWarning = new DiagnosticDescriptor(
            id: "OMNI027",
            title: "Design: MonoBehaviour Network Variable",
            messageFormat: "The Network Variable '{0}' of type '{1}' inherits from MonoBehaviour which cannot be properly serialized over the network. Consider creating a serializable data class instead.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MonoBehaviour and its derivatives cannot be properly serialized for network transmission:" +
                        "\n1. MonoBehaviour contains Unity-specific references that don't serialize" +
                        "\n2. Component references may be invalid across network instances" +
                        "\n3. Scene-specific data may cause desynchronization" +
                        "\n\nRecommended actions:" +
                        "\n- Create a plain C# class (POCO) to hold the network data" +
                        "\n- Implement IMessage interface" +
                        "\n- Use primitive types or serializable structures"
        );

        public static readonly DiagnosticDescriptor StaticNetworkVariable = new DiagnosticDescriptor(
            id: "OMNI030",
            title: "Network Variable Should Not Be Static",
            messageFormat: "The Network Variable '{0}' should not be static as it is automatically synchronized across instances",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private readonly DiagnosticDescriptor[] descriptors = new DiagnosticDescriptor[] {
            GenHelper.InvalidFieldNamingConventionIsUpper,
            GenHelper.InvalidFieldNamingConventionStartsWith,
            GenHelper.InheritanceConstraintViolation,
            GenHelper.PartialKeywordMissing,
            NetworkVariableFieldShouldBePrivate,
            NonValueTypeEqualityCheck,
            HighBandwidthUsageWarning,
            ClientAuthorityWarning,
            RequiresOwnershipWarning,
            ClientAuthorityWithoutOwnershipWarning,
            CollectionTypeWarning,
            BooleanCountWarning,
            MonoBehaviourSerializationWarning,
            StaticNetworkVariable
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptors);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ClassDeclarationSyntax @class)
            {
                int booleanCount = 0;
                var fields = @class.GetDescendantsOfType<FieldDeclarationSyntax>().Where(f => f.HasAttribute("NetworkVariable")).ToArray();
                foreach (var field in fields)
                {
                    string typeName = field.Declaration.Type.ToString();
                    if (typeName == "bool" || typeName == "Boolean")
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            booleanCount++;
                        }
                    }
                }

                foreach (var field in fields)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (booleanCount >= 4)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    BooleanCountWarning,
                                    variable.GetLocation(),
                                    @class.Identifier.Text,
                                    booleanCount
                                )
                            );
                        }
                    }
                }
            }
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is FieldDeclarationSyntax field)
            {
                if (field.HasAttribute("NetworkVariable"))
                {
                    Context cContext = new Context(context);
                    if (field.Parent is ClassDeclarationSyntax @class)
                    {
                        GenHelper.ReportPartialKeywordRequirement(cContext, @class, field.GetLocation());
                        if (!field.HasModifier(SyntaxKind.PrivateKeyword))
                        {
                            foreach (var variable in field.Declaration.Variables)
                            {
                                cContext.ReportDiagnostic(
                                    NetworkVariableFieldShouldBePrivate,
                                    variable.GetLocation(),
                                    variable.Identifier.Text
                                );
                            }
                        }

                        if (field.HasModifier(SyntaxKind.StaticKeyword))
                        {
                            foreach (var variable in field.Declaration.Variables)
                            {
                                cContext.ReportDiagnostic(
                                    StaticNetworkVariable,
                                    variable.GetLocation(),
                                    variable.Identifier.Text
                                );
                            }
                        }

                        var model = context.SemanticModel;
                        var typeInfo = model.GetTypeInfo(field.Declaration.Type);
                        if (typeInfo.Type != null)
                        {
                            bool isValueType = typeInfo.Type.IsValueType;
                            void ReportNonValueTypeEqualityCheck()
                            {
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonValueTypeEqualityCheck,
                                            variable.GetLocation(),
                                            variable.Identifier.Text
                                        )
                                    );
                                }
                            }

                            bool requiresOwnership = true;
                            bool isClientAuthority = false;

                            AttributeSyntax attribute = field.GetAttribute("NetworkVariable");
                            if (attribute != null)
                            {
                                var requiresOwnershipExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("RequiresOwnership", ArgumentIndex.None);
                                if (requiresOwnershipExpression != null)
                                {
                                    if (bool.TryParse(requiresOwnershipExpression.Token.ValueText, out requiresOwnership))
                                    {
                                        if (!requiresOwnership)
                                        {
                                            foreach (var variable in field.Declaration.Variables)
                                            {
                                                context.ReportDiagnostic(
                                                    Diagnostic.Create(
                                                        RequiresOwnershipWarning,
                                                        variable.GetLocation(),
                                                        variable.Identifier.Text
                                                    )
                                                );
                                            }
                                        }
                                    }
                                }

                                var isClientAuthorityExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("IsClientAuthority", ArgumentIndex.None);
                                if (isClientAuthorityExpression != null)
                                {
                                    if (bool.TryParse(isClientAuthorityExpression.Token.ValueText, out isClientAuthority))
                                    {
                                        if (isClientAuthority)
                                        {
                                            foreach (var variable in field.Declaration.Variables)
                                            {
                                                context.ReportDiagnostic(
                                                    Diagnostic.Create(
                                                        ClientAuthorityWarning,
                                                        variable.GetLocation(),
                                                        variable.Identifier.Text
                                                    )
                                                );
                                            }
                                        }
                                    }
                                }

                                if (!requiresOwnership && isClientAuthority)
                                {
                                    foreach (var variable in field.Declaration.Variables)
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                ClientAuthorityWithoutOwnershipWarning,
                                                variable.GetLocation(),
                                                variable.Identifier.Text
                                            )
                                        );
                                    }
                                }

                                var equalExpression = attribute.GetArgumentExpression<LiteralExpressionSyntax>("CheckEquality", ArgumentIndex.None);
                                if (equalExpression != null)
                                {
                                    if (bool.TryParse(equalExpression.Token.ValueText, out bool isEquality))
                                    {
                                        if (!isValueType)
                                        {
                                            if (isEquality)
                                            {
                                                ReportNonValueTypeEqualityCheck();
                                            }
                                        }
                                        else
                                        {
                                            if (!isEquality)
                                            {
                                                foreach (var variable in field.Declaration.Variables)
                                                {
                                                    context.ReportDiagnostic(
                                                        Diagnostic.Create(
                                                            HighBandwidthUsageWarning,
                                                            variable.GetLocation(),
                                                            variable.Identifier.Text
                                                        )
                                                    );
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!isValueType)
                                    {
                                        ReportNonValueTypeEqualityCheck();
                                    }
                                }
                            }

                            if (typeInfo.Type.InheritsFromClass("MonoBehaviour"))
                            {
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            MonoBehaviourSerializationWarning,
                                            variable.GetLocation(),
                                            variable.Identifier.Text,
                                            typeInfo.Type.ToString()
                                        )
                                    );
                                }
                            }

                            string typeName = typeInfo.Type.ToString();
                            if (typeName.StartsWith("System.Collections.Generic.Dictionary<"))
                            {
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            CollectionTypeWarning,
                                            variable.GetLocation(),
                                            variable.Identifier.Text,
                                            "Dictionary<K,V>",
                                            "ObservableDictionary<K,V>"
                                        )
                                    );
                                }
                            }
                            else if (typeName.StartsWith("System.Collections.Generic.List<"))
                            {
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            CollectionTypeWarning,
                                            variable.GetLocation(),
                                            variable.Identifier.Text,
                                            "List<T>",
                                            "ObservableList<T>"
                                        )
                                    );
                                }
                            }
                        }

                        bool isNetworkBehaviour = @class.InheritsFromClass(model, "NetworkBehaviour");
                        bool isClientBehaviour = @class.InheritsFromClass(model, "ClientBehaviour");
                        bool isServerBehaviour = @class.InheritsFromClass(model, "ServerBehaviour");

                        foreach (var variable in field.Declaration.Variables)
                        {
                            var location = variable.GetLocation();
                            string fieldName = variable.Identifier.Text;
                            string fieldNameWithoutPrefix = fieldName.Length > 2 ? fieldName.Substring(2) : fieldName;

                            if (!GenHelper.ReportInvalidFieldNamingStartsWith(cContext, fieldName, location))
                            {
                                if (!GenHelper.ReportInvalidFieldNamingIsUpper(cContext, fieldNameWithoutPrefix, location))
                                {
                                    // Note: DualBehaviour is not supported. It is a manual class that should not be used with auto-generated properties.
                                    if (!isNetworkBehaviour && !isClientBehaviour && !isServerBehaviour)
                                    {
                                        GenHelper.ReportInheritanceRequirement(cContext, @class.Identifier.Text, location);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}