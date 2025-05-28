using FlowSynx.PluginCore;
using System.Text.Json;

namespace FlowPack;

public static class PluginReflector
{
    private const string PluginSearchPattern = "*.dll";

    public static PluginMetadata? ExtractPluginMetadata(string pluginDirectory)
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
                Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                loader.Unload();
            }
        }

        return null;


        //foreach (var dllPath in Directory.GetFiles(pluginDirectory, "*.dll"))
        //{
        //    try
        //    {
        //        var assembly = Assembly.LoadFrom(dllPath);

        //        foreach (var type in assembly.GetTypes())
        //        {
        //            if (!type.IsClass || type.IsAbstract || type.IsInterface)
        //                continue;

        //            if (!interfaceType.IsAssignableFrom(type))
        //                continue;

        //            var instance = Activator.CreateInstance(type);
        //            if (instance == null)
        //                continue;

        //            var metadata = new PluginMetadata
        //            {
        //                Id = GetPropertyValue<Guid>(type, instance, nameof(PluginMetadata.Id)),
        //                Type = GetPropertyValue<string>(type, instance, nameof(PluginMetadata.Type)),
        //                Version = GetPropertyValue<string>(type, instance, nameof(PluginMetadata.Version)),
        //                CompanyName = GetPropertyValue<string>(type, instance, nameof(PluginMetadata.CompanyName)),
        //                Description = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.Description)),
        //                License = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.License)),
        //                LicenseUrl = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.LicenseUrl)),
        //                Icon = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.Icon)),
        //                ProjectUrl = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.ProjectUrl)),
        //                RepositoryUrl = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.RepositoryUrl)),
        //                Copyright = GetPropertyValue<string?>(type, instance, nameof(PluginMetadata.Copyright)),
        //                Authors = GetPropertyValue<List<string>>(type, instance, nameof(PluginMetadata.Authors)) ?? new(),
        //                Tags = GetPropertyValue<List<string>>(type, instance, nameof(PluginMetadata.Tags)) ?? new()
        //            };

        //            return metadata;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($"Failed to read metadata from {dllPath}: {ex.Message}");
        //    }
        //}

        //return null;
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
            Authors = plugin.Metadata.Authors ?? new(),
            Tags = plugin.Metadata.Tags ?? new()
        };
    }

    private static T? GetPropertyValue<T>(Type type, object instance, string propertyName)
    {
        var prop = type.GetProperty(propertyName);
        if (prop == null || !typeof(T).IsAssignableFrom(prop.PropertyType))
            return default;

        return (T?)prop.GetValue(instance);
    }

    public static string SaveMetadataToFile(PluginMetadata metadata, string outputDirectory)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(outputDirectory, "manifest.json");
        File.WriteAllText(path, json);
        return path;
    }
}