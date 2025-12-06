using Xunit;

namespace FlowPack.UnitTests;

public class PluginMetadataTests
{
    [Fact]
    public void PluginMetadata_PropertyAssignment_Works()
    {
        var meta = new PluginMetadata
        {
            Id = Guid.NewGuid(),
            Type = "MyPlugin",
            Version = "1.0.0",
            CompanyName = "FlowSynx",
            CategoryId = "cat-01",
            MinimumFlowSynxVersion = "1.4.0",
            Description = "desc",
            License = "MIT",
            LicenseUrl = "https://example.com/license",
            Icon = "icon.png",
            ProjectUrl = "https://example.com/project",
            RepositoryUrl = "https://example.com/repo",
            Copyright = "©",
            ReadMe = "README content",
            TargetFlowSynxVersion = "1.5.0"
        };
        meta.Authors.Add("Alice");
        meta.Tags.Add("tag1");
        meta.Specifications.Add(new SpecificationMetadata());
        meta.Operations.Add(new PluginOperationMetadata());

        Assert.Equal("MyPlugin", meta.Type);
        Assert.Equal("1.0.0", meta.Version);
        Assert.Equal("FlowSynx", meta.CompanyName);
        Assert.Contains("Alice", meta.Authors);
        Assert.Contains("tag1", meta.Tags);
        Assert.Single(meta.Specifications);
        Assert.Single(meta.Operations);
    }
}