using Microsoft.Extensions.FileProviders;
using LibraryDiagnostic = TheODI.NamedQuery.Validation.Diagnostic;

namespace TheODI.NamedQuery.Validator;

internal class SourceLocator(Arguments arguments, IFileProvider definitions)
{
    private readonly IReadOnlyList<string> sources = arguments.Sources is { } path && File.Exists(path)
        ? File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => Path.GetFullPath(line.Trim())).ToList()
        : [];

    internal Diagnostic Locate(LibraryDiagnostic diagnostic) => (Find(diagnostic.Path) is { } origin
        ? new Diagnostic(origin, diagnostic.Message)
        : new Diagnostic(arguments.Assembly, string.IsNullOrEmpty(diagnostic.Path) ? diagnostic.Message : $"{diagnostic.Path}: {diagnostic.Message}"))
    with
    {
        Code = diagnostic.Code,
        Line = diagnostic.Line,
        Column = diagnostic.Column,
        EndLine = diagnostic.EndLine,
        EndColumn = diagnostic.EndColumn
    };

    private static byte[] Bytes(IFileInfo file)
    {
        using var stream = file.CreateReadStream();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private string? Find(string path)
    {
        if (string.IsNullOrEmpty(path) || definitions.GetFileInfo(path) is not { Exists: true } file)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(file.PhysicalPath))
        {
            return file.PhysicalPath;
        }

        var suffix = Path.DirectorySeparatorChar + path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var candidates = sources.Where(source => source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count <= 1)
        {
            return candidates.SingleOrDefault();
        }

        var content = Bytes(file);
        var matches = candidates.Where(candidate => File.Exists(candidate) && File.ReadAllBytes(candidate).AsSpan().SequenceEqual(content)).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
