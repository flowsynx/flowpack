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

            specInstance = CreateInstanceBestEffort(specType, plugin);
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
                .FirstOrDefault(i => i.IsGenericType &&
                                     i.GetGenericTypeDefinition().Name == "IPluginOperation`2");

            if (opInterface == null)
                continue;

            var opInstance = CreateInstanceBestEffort(type, plugin);
            if (opInstance == null)
                continue;

            var nameProp = type.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
            var descProp = type.GetProperty("Description", BindingFlags.Instance | BindingFlags.Public);

            var opMeta = new PluginOperationMetadata
            {
                Name = nameProp?.GetValue(opInstance)?.ToString() ?? type.Name,
                Description = descProp?.GetValue(opInstance)?.ToString()
            };

            var paramType = opInterface.GetGenericArguments()[0];
            List<PluginOperationParameterMetadata> paramMetas = new();

            // Try creating parameter objects too
            var paramTypeInstance = CreateInstanceBestEffort(paramType);

            foreach (var prop in paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = prop.GetCustomAttribute<OperationParameterMetadataAttribute>();
                if (attr == null)
                    continue;

                paramMetas.Add(new PluginOperationParameterMetadata
                {
                    Name = prop.Name,
                    Description = attr.Description,
                    Type = TypeExtensions.GetCanonicalAiTypeName(prop.PropertyType),
                    DefaultValue = paramTypeInstance != null
                                       ? prop.GetValue(paramTypeInstance)?.ToString()
                                       : null,
                    IsRequired = attr.IsRequired
                });
            }

            opMeta.Parameters = paramMetas;
            operations.Add(opMeta);
        }

        return operations;
    }

    private static object? CreateInstanceBestEffort(Type type, object? plugin = null)
    {
        // Try simplest path first
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            // ignore and try advanced logic
        }

        // Choose the "best" constructor: most parameters, public only
        var ctors = type.GetConstructors()
                        .OrderByDescending(c => c.GetParameters().Length)
                        .ToList();

        foreach (var ctor in ctors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            bool failed = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                // 1. If plugin instance is assignable to parameter → use it
                if (plugin != null && p.ParameterType.IsAssignableFrom(plugin.GetType()))
                {
                    args[i] = plugin;
                    continue;
                }

                // 2. If parameter has default value → use it
                if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                    continue;
                }

                // 3. If nullable reference/value → pass null
                if (!p.ParameterType.IsValueType || Nullable.GetUnderlyingType(p.ParameterType) != null)
                {
                    args[i] = null;
                    continue;
                }

                // 4. If parameter type has parameterless constructor → build it
                try
                {
                    args[i] = Activator.CreateInstance(p.ParameterType);
                    continue;
                }
                catch
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
                continue;

            try
            {
                return ctor.Invoke(args);
            }
            catch
            {
                // try next constructor
            }
        }

        return null; // No usable constructor
    }

    public static string SaveMetadataToFile(PluginMetadata metadata, string outputDirectory)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(outputDirectory, "metadata.json");
        File.WriteAllText(path, json);
        return path;
    }
}