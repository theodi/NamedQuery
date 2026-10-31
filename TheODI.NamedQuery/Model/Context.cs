namespace TheODI.NamedQuery.Model;

internal class Context
{
    public Endpoint? Endpoint { get; set; }

    public string? Query { get; set; }

    public object? Result { get; set; }
}
