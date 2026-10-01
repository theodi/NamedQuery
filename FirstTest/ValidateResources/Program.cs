using ValidateResources;

try
{
	Validator.Validate();
	return 0;
}
catch (MsBuildCanonicalErrorException e)
{
	Console.Error.WriteLine(e);
	return 1;
}
catch (Exception e)
{
	Console.Error.WriteLine($"ValidateResources : error: {e.Message}");
	return 1;
}
