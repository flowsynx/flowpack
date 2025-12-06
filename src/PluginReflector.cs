using FlowSynx.PluginCore;
using System.Reflection;
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

    private static PluginMetadata CreatePluginMetadata(IPlugin plugin) => new PluginMetadata
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
        CategoryId = plugin.Metadata.Category.ToString(),
        MinimumFlowSynxVersion = plugin.Metadata.MinimumFlowSynxVersion.ToString(),
        TargetFlowSynxVersion = plugin.Metadata.TargetFlowSynxVersion == null 
                                ? "" 
                                : plugin.Metadata.TargetFlowSynxVersion.ToString(),
        Specifications = ExtractSpecificationMetadata(plugin),
        Operations = ExtractOperations(plugin)
    };

    private static List<SpecificationMetadata> ExtractSpecificationMetadata(IPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        var result = new List<SpecificationMetadata>();

        // Try to find the concrete Specifications type
        var specProperty = plugin.GetType().GetProperty("Specifications", BindingFlags.Public | BindingFlags.Instance);
        if (specProperty == null)
            return result;

        Type specType;

        // If the property is already instantiated, use its type
        var specInstance = specProperty.GetValue(plugin);
        if (specInstance != null)
        {
            specType = specInstance.GetType();
        }
        else
        {
            var declaredType = specProperty.PropertyType;
            specType = plugin.GetType().Assembly
                .GetTypes()
                .FirstOrDefault(t =>
                    declaredType.IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.IsClass
                ) ?? throw new InvalidOperationException("No concrete Specifications class found.");

            // Create an instance to get default values
            specInstance = Activator.CreateInstance(specType);
        }

        // Iterate over properties of the concrete type
        foreach (var prop in specType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<SpecificationMetadataAttribute>();
            if (attr == null)
                continue;

            var defaultValue = specInstance != null ? prop.GetValue(specInstance) : null;

            result.Add(new SpecificationMetadata
            {
                Name = prop.Name,
                Description = attr.Description,
                Type = TypeExtensions.GetCanonicalAiTypeName(prop.PropertyType),
                DefaultValue = defaultValue?.ToString(),
                IsRequired = attr.IsRequired
            });
        }

        return result;
    }

    private static List<PluginOperationMetadata> ExtractOperations(IPlugin plugin)
    {
        var operations = new List<PluginOperationMetadata>();
        var asm = plugin.GetType().Assembly;

        foreach (var type in asm.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var opInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IPluginOperation`2");

            if (opInterface == null)
                continue;

            object? opInstance = null;
            try { opInstance = Activator.CreateInstance(type); } catch { continue; }

            var nameProp = type.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
            var descProp = type.GetProperty("Description", BindingFlags.Instance | BindingFlags.Public);

            var opMeta = new PluginOperationMetadata
            {
                Name = nameProp?.GetValue(opInstance)?.ToString() ?? type.Name,
                Description = descProp?.GetValue(opInstance)?.ToString()
            };

            var paramType = opInterface.GetGenericArguments()[0];
            List<PluginOperationParameterMetadata> paramMetas = new();

            foreach (var prop in paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = prop.GetCustomAttribute<OperationParameterMetadataAttribute>();
                if (attr == null) continue;

                object? paramInstance = null;
                try { paramInstance = Activator.CreateInstance(paramType); } catch { }

                paramMetas.Add(new PluginOperationParameterMetadata
                {
                    Name = prop.Name,
                    Description = attr.Description,
                    Type = TypeExtensions.GetCanonicalAiTypeName(prop.PropertyType),
                    DefaultValue = paramInstance != null ? prop.GetValue(paramInstance)?.ToString() : null,
                    IsRequired = attr.IsRequired
                });
            }

            opMeta.Parameters = paramMetas;
            operations.Add(opMeta);
        }

        return operations;
    }

    public static string SaveMetadataToFile(PluginMetadata metadata, string outputDirectory)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(outputDirectory, "metadata.json");
        File.WriteAllText(path, json);
        return path;
    }
}