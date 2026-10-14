namespace Web.Model;

public partial class Manifest
{
    public Endpoint? this[string path] => Endpoints.SingleOrDefault(endpoint => endpoint.Path == path);
}
