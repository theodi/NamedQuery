using ValidateResources;

try
{
	Validator.Validate();
	return 0;
}
catch (Exception e)
{
	Console.Error.WriteLine($"ValidateResources : error: {e.Message}");
	return 1;
}
