namespace TheODI.NamedQuery.Validator;

internal record Arguments(string Assembly, string? Sources, string? InputsOut, TimeSpan Timeout)
{
    internal static Arguments? Parse(string[] args)
    {
        string? assembly = null;
        string? sources = null;
        string? inputsOut = null;
        var timeout = TimeSpan.FromSeconds(60);

        for (var i = 0; i < args.Length; i++)
        {
            if (i + 1 == args.Length)
            {
                return null;
            }

            var value = args[++i];
            switch (args[i - 1])
            {
                case "--assembly":
                    assembly = Path.GetFullPath(value);
                    break;
                case "--sources":
                    sources = Path.GetFullPath(value);
                    break;
                case "--inputs-out":
                    inputsOut = Path.GetFullPath(value);
                    break;
                case "--timeout" when int.TryParse(value, out var seconds) && seconds > 0:
                    timeout = TimeSpan.FromSeconds(seconds);
                    break;
                default:
                    return null;
            }
        }

        return assembly is null ? null : new Arguments(assembly, sources, inputsOut, timeout);
    }
}
