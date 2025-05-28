using System.Reflection;

namespace FlowPack;

public static class VersionHelper
{
    public static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return versionAttr?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "unknown";
    }
}