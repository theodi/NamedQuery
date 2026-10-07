namespace Test;

internal class MyWebApplication : WebApplicationFactory<Program>
{
	private HttpClient? client;

	internal HttpClient Client => client ??= CreateClient();
}
