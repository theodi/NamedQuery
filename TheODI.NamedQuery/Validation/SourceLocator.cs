using Microsoft.Extensions.FileProviders;

namespace TheODI.NamedQuery.Validation;

internal class SourceLocator(string? sourcesPath, string assemblyPath, IFileProvider definitions)
{
    private readonly IReadOnlyList<string> sources = sourcesPath is { } path && File.Exists(path)
        ? File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => Path.GetFullPath(line.Trim())).ToList()
        : [];

    internal Diagnostic Locate(Diagnostic diagnostic) => Find(diagnostic.Path) is { } origin
        ? diagnostic with { Path = origin }
        : diagnostic with { Path = assemblyPath, Message = string.IsNullOrEmpty(diagnostic.Path) ? diagnostic.Message : $"{diagnostic.Path}: {diagnostic.Message}" };

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
