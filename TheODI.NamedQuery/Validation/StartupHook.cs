internal static class StartupHook
{
    public static void Initialize() => TheODI.NamedQuery.Validation.ValidationHook.Initialize();
}
