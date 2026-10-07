namespace Test;

[TestClass]
public sealed class DefaultControllerTests
{
	private static readonly MyWebApplication app = new();

	public required TestContext TestContext { get; set; }

	[TestMethod]
	public async Task ServesLocalDevelopmentEndpointDefinitions()
	{
		var response = await app.Client.GetAsync("", TestContext.CancellationToken);

		response.Should().Be200Ok();
	}
}
