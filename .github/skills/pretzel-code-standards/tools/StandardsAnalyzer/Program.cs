using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Pretzel.StandardsAnalyzer;

/// <summary>
/// Type-aware checks the line-based script cannot do: string comparisons without explicit semantics and string-keyed
/// collections without a comparer. Compiles every package source against the Unity Editor and a host project's assemblies.
/// </summary>
internal static class Program
{
    private static readonly string[] _comparisonMethods =
    {
        "Equals", "StartsWith", "EndsWith", "IndexOf", "LastIndexOf", "Contains", "Compare", "CompareTo",
    };

    private static readonly string[] _keyedCollections =
    {
        "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.SortedDictionary`2", "System.Collections.Generic.SortedSet`1",
        "System.Collections.Concurrent.ConcurrentDictionary`2",
    };

    private static int Main(string[] args)
    {
        string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
        string editor = args.Length > 1 ? args[1] : @"D:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor";
        string host = args.Length > 2 ? Path.GetFullPath(args[2]) : Path.Combine(root, "TestPretzel");

        if (!Directory.Exists(Path.Combine(editor, "Data", "Managed")))
        {
            Console.Error.WriteLine($"Unity Editor not found at '{editor}'. Pass the Editor folder as the second argument.");
            return 2;
        }

        string packages = Path.Combine(root, "Packages");
        List<string> files = Directory.EnumerateFiles(packages, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9).WithPreprocessorSymbols(GetDefines(packages));
        List<SyntaxTree> trees = new();
        foreach (string file in files)
        {
            trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), parseOptions, file));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "PretzelStandards",
            trees,
            GetReferences(editor, host),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        List<string> findings = new();
        bool showCompileErrors = args.Contains("--compile", StringComparer.Ordinal);
        foreach (Diagnostic diagnostic in compilation.GetDiagnostics())
        {
            string message = diagnostic.GetMessage();
            if (showCompileErrors && diagnostic.Severity == DiagnosticSeverity.Error)
            {
                FileLinePositionSpan any = diagnostic.Location.GetLineSpan();
                Console.WriteLine($"compile {Path.GetRelativePath(root, any.Path).Replace('\\', '/')}:{any.StartLinePosition.Line + 1}: {diagnostic.Id} {message}");
            }

            if (diagnostic.Severity == DiagnosticSeverity.Error
                && (diagnostic.Id == "CS0104"
                    || message.Contains("'StringComparison'", StringComparison.Ordinal)
                    || message.Contains("'StringComparer'", StringComparison.Ordinal)))
            {
                FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
                string relative = Path.GetRelativePath(root, span.Path).Replace('\\', '/');
                string rule = diagnostic.Id == "CS0104" ? "ambiguous-reference" : "missing-using";
                findings.Add($"{relative}:{span.StartLinePosition.Line + 1}: [{rule}] {message}");
            }
        }

        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                string? rule = Check(node, model);
                if (rule != null)
                {
                    FileLinePositionSpan span = node.GetLocation().GetLineSpan();
                    string relative = Path.GetRelativePath(root, tree.FilePath).Replace('\\', '/');
                    findings.Add($"{relative}:{span.StartLinePosition.Line + 1}: [{rule}] {node.ToString().Trim()}");
                }
            }
        }

        foreach (string finding in findings.Distinct(StringComparer.Ordinal))
        {
            Console.WriteLine("error " + finding);
        }

        Console.WriteLine($"{files.Count} files, {findings.Count} type-aware errors");
        return findings.Count > 0 ? 1 : 0;
    }

    private static string? Check(SyntaxNode node, SemanticModel model)
    {
        if (node is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && !IsNullOrDefault(binary.Left) && !IsNullOrDefault(binary.Right)
            && model.GetSymbolInfo(binary).Symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator or MethodKind.UserDefinedOperator } op
            && op.ContainingType?.SpecialType == SpecialType.System_String)
        {
            return "string-comparison";
        }

        if (node is InvocationExpressionSyntax invocation
            && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
            && method.ContainingType?.SpecialType == SpecialType.System_String
            && _comparisonMethods.Contains(method.Name, StringComparer.Ordinal)
            && method.Parameters.Length > 0
            && method.Parameters[0].Type.SpecialType != SpecialType.System_Char
            && !method.Parameters.Any(p => IsComparison(p.Type)))
        {
            return "string-comparison";
        }

        if (node is InvocationExpressionSyntax lookup
            && model.GetSymbolInfo(lookup).Symbol is IMethodSymbol contains
            && contains.Name is "Contains" or "IndexOf" or "Distinct"
            && contains.ContainingType?.SpecialType != SpecialType.System_String
            && IsStringSequence(contains)
            && !contains.Parameters.Any(p => IsComparison(p.Type)))
        {
            return "string-comparer";
        }

        if (node is BaseObjectCreationExpressionSyntax creation
            && model.GetOperation(creation) is IObjectCreationOperation { Constructor: { } constructor }
            && constructor.ContainingType is { IsGenericType: true } type
            && _keyedCollections.Contains(MetadataName(type.OriginalDefinition), StringComparer.Ordinal)
            && type.TypeArguments[0].SpecialType == SpecialType.System_String
            && !constructor.Parameters.Any(p => IsComparison(p.Type)))
        {
            return "string-comparer";
        }

        return null;
    }

    // List<string>.Contains and LINQ Contains/Distinct over strings use the default (ordinal) equality. Sets carry their own comparer.
    private static bool IsStringSequence(IMethodSymbol method)
    {
        if (method.IsExtensionMethod || method.ReducedFrom != null)
        {
            IMethodSymbol definition = method.ReducedFrom ?? method;
            return string.Equals(definition.ContainingType.ToDisplayString(), "System.Linq.Enumerable", StringComparison.Ordinal)
                && method.TypeArguments.Length > 0
                && method.TypeArguments[0].SpecialType == SpecialType.System_String;
        }

        return method.ContainingType is { IsGenericType: true } owner
            && !_keyedCollections.Contains(MetadataName(owner.OriginalDefinition), StringComparer.Ordinal)
            && owner.TypeArguments.Length == 1
            && owner.TypeArguments[0].SpecialType == SpecialType.System_String;
    }

    private static bool IsNullOrDefault(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression) || expression.IsKind(SyntaxKind.DefaultLiteralExpression);
    }

    private static bool IsComparison(ITypeSymbol type)
    {
        string name = type.OriginalDefinition.ToDisplayString();
        return name is "System.StringComparison" or "System.StringComparer"
            || name.StartsWith("System.Collections.Generic.IEqualityComparer<", StringComparison.Ordinal)
            || name.StartsWith("System.Collections.Generic.IComparer<", StringComparison.Ordinal);
    }

    private static string MetadataName(INamedTypeSymbol type) => $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";

    private static IEnumerable<string> GetDefines(string packages)
    {
        HashSet<string> defines = new(StringComparer.Ordinal)
        {
            "UNITY_EDITOR", "UNITY_INCLUDE_TESTS", "UNITY_2021_3_OR_NEWER", "UNITY_2022_3_OR_NEWER", "UNITY_2023_1_OR_NEWER",
            "UNITY_6000_0_OR_NEWER", "UNITY_6000_2_OR_NEWER", "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "ENABLE_INPUT_SYSTEM",
        };
        Regex define = new("\"define\"\\s*:\\s*\"(\\w+)\"");
        foreach (string asmdef in Directory.EnumerateFiles(packages, "*.asmdef", SearchOption.AllDirectories))
        {
            foreach (Match match in define.Matches(File.ReadAllText(asmdef)))
            {
                defines.Add(match.Groups[1].Value);
            }
        }

        return defines;
    }

    private static ImmutableArray<MetadataReference> GetReferences(string editor, string host)
    {
        List<string> candidates = new();
        string data = Path.Combine(editor, "Data");
        candidates.Add(Path.Combine(data, "NetStandard", "ref", "2.1.0", "netstandard.dll"));
        AddDlls(candidates, Path.Combine(data, "NetStandard", "compat", "2.1.0", "shims", "netfx"));
        AddDlls(candidates, Path.Combine(data, "Managed", "UnityEngine"));
        AddDlls(candidates, Path.Combine(host, "Library", "ScriptAssemblies"));
        AddDlls(candidates, Path.Combine(host, "Library", "PackageCache"), recursive: true);

        Dictionary<string, MetadataReference> references = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in candidates)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (references.ContainsKey(name) || name.StartsWith("Pretzel.", StringComparison.Ordinal) || !HasMetadata(file))
            {
                continue;
            }

            references[name] = MetadataReference.CreateFromFile(file);
        }

        return references.Values.ToImmutableArray();
    }

    private static void AddDlls(List<string> files, string folder, bool recursive = false)
    {
        if (Directory.Exists(folder))
        {
            files.AddRange(Directory.EnumerateFiles(folder, "*.dll", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
        }
    }

    private static bool HasMetadata(string file)
    {
        try
        {
            using FileStream stream = File.OpenRead(file);
            using PEReader reader = new(stream);
            return reader.HasMetadata && reader.GetMetadataReader().IsAssembly;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }
}
