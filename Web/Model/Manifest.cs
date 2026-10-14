namespace Web.Model;

public partial class Manifest
{
    public Endpoint? this[string path] => Node.Endpoint.SingleOrDefault(endpoint => endpoint.Path == path);
}
