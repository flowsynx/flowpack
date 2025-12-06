using Xunit;

namespace FlowPack.UnitTests;

public class PackOptionsTests
{
    [Fact]
    public void Parse_SetsProjectPath_WhenCsprojProvided()
    {
        var args = new[] { "C:\\proj\\myapp.csproj" };
        var options = PackOptions.Parse(args);
        Assert.Equal("C:\\proj\\myapp.csproj", options.ProjectPath);
        Assert.False(options.Clean);
        Assert.False(options.Verbose);
        Assert.Null(options.OutputPath);
    }

    [Fact]
    public void Parse_SetsOutputPath_WhenOutputFlagProvided()
    {
        var args = new[] { "--output", "C:\\out\\myplugin.fspack", "C:\\proj\\myapp.csproj" };
        var options = PackOptions.Parse(args);
        Assert.Equal("C:\\out\\myplugin.fspack", options.OutputPath);
        Assert.Equal("C:\\proj\\myapp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_SetsCleanFlag_WhenCleanProvided()
    {
        var args = new[] { "--clean", "C:\\proj\\myapp.csproj" };
        var options = PackOptions.Parse(args);
        Assert.True(options.Clean);
        Assert.Equal("C:\\proj\\myapp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_SetsVerboseFlag_WhenVerboseProvided()
    {
        var args = new[] { "--verbose", "C:\\proj\\myapp.csproj" };
        var options = PackOptions.Parse(args);
        Assert.True(options.Verbose);
        Assert.Equal("C:\\proj\\myapp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_IgnoresOutputFlag_WithoutValue()
    {
        var args = new[] { "--output", "--clean", "C:\\proj\\myapp.csproj" };
        var options = PackOptions.Parse(args);
        Assert.Null(options.OutputPath);
        Assert.True(options.Clean);
        Assert.Equal("C:\\proj\\myapp.csproj", options.ProjectPath);
    }

    [Fact]
    public void Parse_DoesNotSetProjectPath_WhenNoCsprojProvided()
    {
        var args = new[] { "--verbose" };
        var options = PackOptions.Parse(args);
        Assert.True(options.Verbose);
        Assert.Equal(default!, options.ProjectPath);
    }

    [Fact]
    public void Parse_IgnoresNonCsprojArguments()
    {
        var args = new[] { "readme.md", "--clean", "solution.sln", "myapp.txt" };
        var options = PackOptions.Parse(args);
        Assert.True(options.Clean);
        Assert.Equal(default!, options.ProjectPath);
    }
}