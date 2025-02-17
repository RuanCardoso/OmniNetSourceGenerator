using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public enum ArgumentIndex
{
    First = 0,
    Second = 1,
    Third = 2,
    Fourth = 3,
    Fifth = 4,
    Sixth = 5,
    Seventh = 6,
    Eighth = 7,
    Ninth = 8,
    None = First
     + Second
     + Third
     + Fourth
     + Fifth
     + Sixth
     + Seventh
     + Eighth
     + Ninth
}

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

public readonly struct Context
{
    private readonly GeneratorExecutionContext? _context;
    private readonly SyntaxNodeAnalysisContext? _syntaxContext;

    public Context(GeneratorExecutionContext context)
    {
        _context = context;
        _syntaxContext = null;
    }

    public Context(SyntaxNodeAnalysisContext syntaxContext)
    {
        _syntaxContext = syntaxContext;
        _context = null;
    }

    public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
    {
        if (_context != null) _context.Value.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
        else _syntaxContext?.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }
}
