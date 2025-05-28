using System.Reflection;

namespace FlowPack;

public static class VersionHelper
{
    public static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = versionAttr?.InformationalVersion;

        if (version != null)
        {
            var parts = version.Split(new[] { '-', '+' }, 2);
            return parts[0];
        }

        return "unknown";
    }
}