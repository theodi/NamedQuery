using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using TheODI.NamedQuery.Validator;

if (Arguments.Parse(args) is not { } arguments)
{
    Console.Error.WriteLine("Usage: TheODI.NamedQuery.Validator --assembly <path> [--sources <path>] [--inputs-out <path>] [--timeout <seconds>]");
    return 2;
}

var directories = new[] { AppContext.BaseDirectory, Path.GetDirectoryName(arguments.Assembly)! };
AssemblyLoadContext.Default.Resolving += (context, name) => directories
    .Select(directory => Path.Combine(directory, $"{name.Name}.dll"))
    .Where(File.Exists)
    .Select(context.LoadFromAssemblyPath)
    .FirstOrDefault();

try
{
    return Run(arguments);
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    return 1;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static int Run(Arguments arguments)
{
    foreach (var diagnostic in HostValidator.Validate(arguments))
    {
        Console.Out.WriteLine(diagnostic);
    }

    return 0;
}
