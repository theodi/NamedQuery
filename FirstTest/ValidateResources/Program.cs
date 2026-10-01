try
{
	new ValidateResources.Validator().Validate();
	return 0;
}
catch (Exception e)
{
	Console.Error.WriteLine($"ValidateResources : error: {e.Message}");
	return 1;
}
