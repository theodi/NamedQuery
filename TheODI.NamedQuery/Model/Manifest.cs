namespace TheODI.NamedQuery.Model;

internal partial class Manifest
{
    public Endpoint? this[string path] => Node.Endpoint.SingleOrDefault(endpoint => endpoint.Path == path);
}
