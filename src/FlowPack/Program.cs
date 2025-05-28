using FlowPack;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length == 0 || args.Contains("--version") || args.Contains("-v"))
        {
            Console.WriteLine($"FlowPack v{VersionHelper.GetVersion()}");
            return 0;
        }

        var options = PackOptions.Parse(args);
        if (options.ProjectPath == null || !File.Exists(options.ProjectPath))
        {
            Console.Error.WriteLine($"Error: Valid project or solution file required.");
            PrintUsage();
            return 1;
        }

        if (options.OutputPath != null && !options.OutputPath.EndsWith(".fspack", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Error: --output path must have a '.fspack' extension.");
            return 1;
        }

        var packager = new Packager(options);
        bool success = packager.Build();

        return success ? 0 : 1;
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            Usage: pack <path-to-csproj> [--output <path>] [--clean] [--verbose]

            Options:
              --output <zip-path>   Specify the path for the output zip file.
              --clean               Delete the temporary publish directory after zipping.
              --verbose             Enable detailed output from commands.
              --version, -v         Show flowpack version.
              --help, -h            Show this help message.
            """);
    }
}