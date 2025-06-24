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
                    options.OutputPath = args.Length > i + 1 ? args[++i] : null;
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