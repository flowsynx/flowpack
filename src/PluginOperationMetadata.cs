namespace FlowPack;

public class PluginOperationMetadata
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PluginOperationParameterMetadata> Parameters { get; set; } = new List<PluginOperationParameterMetadata>();
}

public class PluginOperationParameterMetadata
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? DefaultValue { get; set; }
    public bool? IsRequired { get; set; } = false;
}