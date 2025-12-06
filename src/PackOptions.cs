namespace FlowPack;

public class PackOptions
{
    public string ProjectPath { get; set; } = default!;
    public string? OutputPath { get; set; }
    public bool Clean { get; set; }
    public bool Verbose { get; set; }

    public static PackOptions Parse(string[] args)
    {
        var options = new PackOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                    // Only consume the next arg as output if it exists and is not another flag
                    if (args.Length > i + 1 && !(args[i + 1].StartsWith("--", StringComparison.Ordinal)))
                    {
                        options.OutputPath = args[++i];
                    }
                    else
                    {
                        options.OutputPath = null;
                    }
                    break;
                case "--clean":
                    options.Clean = true;
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                default:
                    if (args[i].EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        options.ProjectPath = args[i];
                    break;
            }
        }

        return options;
    }
}