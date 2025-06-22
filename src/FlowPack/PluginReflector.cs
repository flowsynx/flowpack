using FlowSynx.PluginCore;
using System.Text.Json;

namespace FlowPack;

public static class PluginReflector
{
    private const string PluginSearchPattern = "*.dll";

    public static PluginMetadata? ExtractPluginMetadata(string pluginDirectory, bool verbose)
    {
        var interfaceType = typeof(IPlugin);
        foreach (var dllPath in Directory.GetFiles(pluginDirectory, PluginSearchPattern))
        {
            var loader = new TransientPluginLoader(dllPath);
            try
            {
                loader.Load();
                return CreatePluginMetadata(loader.Plugin);
            }
            catch (Exception ex)
            {
                if (verbose)
                    Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                loader.Unload();
            }
        }

        return null;
    }

    private static PluginMetadata CreatePluginMetadata(IPlugin plugin)
    {
        return new PluginMetadata
        {
            Id = plugin.Metadata.Id,
            Type = plugin.Metadata.Type,
            Version = plugin.Metadata.Version.ToString(),
            CompanyName = plugin.Metadata.CompanyName,
            Description = plugin.Metadata.Description,
            License = plugin.Metadata.License,
            LicenseUrl = plugin.Metadata.LicenseUrl,
            Icon = plugin.Metadata.Icon,
            ProjectUrl = plugin.Metadata.ProjectUrl,
            RepositoryUrl = plugin.Metadata.RepositoryUrl,
            Copyright = plugin.Metadata.Copyright,
            ReadMe = plugin.Metadata.ReadMe,
            Authors = plugin.Metadata.Authors ?? new(),
            Tags = plugin.Metadata.Tags ?? new(),
            CategoryId = plugin.Metadata.Category.ToString()
        };
    }

    public static string SaveMetadataToFile(PluginMetadata metadata, string outputDirectory)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(outputDirectory, "manifest.json");
        File.WriteAllText(path, json);
        return path;
    }
}