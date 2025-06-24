using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace FlowPack;

public class Packager
{
    private readonly PackOptions _options;

    public Packager(PackOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool Build()
    {
        if (!ValidateProjectPath(out var projectName))
            return false;

        var configuration = "Release";
        var tempRoot = CreateTempDirectory("publish_output_");
        var outputDir = CreateSubDirectory(tempRoot);
        var manifestDir = CreateSubDirectory(tempRoot);

        try
        {
            if (_options.Clean && !RunCommand("dotnet", $"clean \"{_options.ProjectPath}\" -c {configuration}"))
                return false;

            if (!RunCommand("dotnet", $"build \"{_options.ProjectPath}\" -c {configuration}"))
                return false;

            if (!RunCommand("dotnet", $"publish \"{_options.ProjectPath}\" -c {configuration} -o \"{outputDir}\""))
                return false;

            var pluginPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".plugin");
            var checksumPath = pluginPath + ".sha256";

            var metadata = PluginReflector.ExtractPluginMetadata(outputDir, _options.Verbose);
            if (metadata == null)
            {
                LogError("No valid plugin metadata found.");
                return false;
            }

            var manifestPath = PluginReflector.SaveMetadataToFile(metadata, manifestDir);

            ZipFile.CreateFromDirectory(outputDir, pluginPath);
            LogInfo($"Plugin created: {pluginPath}");

            var checksum = ComputeSha256(pluginPath);
            File.WriteAllText(checksumPath, checksum);
            LogInfo($"SHA256: {checksum}");

            var finalPackagePath = ResolveOutputPath(projectName);
            CreateFinalPackage(finalPackagePath, pluginPath, manifestPath, checksumPath, projectName);

            LogInfo($"Final package created: {finalPackagePath}");
            return true;
        }
        finally
        {
            CleanupTempFiles(tempRoot);
        }
    }

    private bool ValidateProjectPath(out string projectName)
    {
        projectName = Path.GetFileNameWithoutExtension(_options.ProjectPath);

        if (!File.Exists(_options.ProjectPath) || Path.GetExtension(_options.ProjectPath) != ".csproj")
        {
            LogError("Provided file must be a valid .csproj file.");
            return false;
        }

        return true;
    }

    private string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid());
        Directory.CreateDirectory(path);
        return path;
    }

    private string CreateSubDirectory(string parent)
    {
        var subDir = Path.Combine(parent, Guid.NewGuid().ToString());
        Directory.CreateDirectory(subDir);
        return subDir;
    }

    private string ResolveOutputPath(string projectName)
    {
        if (string.IsNullOrWhiteSpace(_options.OutputPath))
        {
            return Path.Combine(Directory.GetCurrentDirectory(), $"{projectName}.fspack");
        }

        if (!Path.GetExtension(_options.OutputPath).Equals(".fspack", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Output file must have a .fspack extension.");
        }

        return _options.OutputPath;
    }

    private void CreateFinalPackage(string packagePath, string pluginPath, string manifestPath, string checksumPath, string projectName)
    {
        if (File.Exists(packagePath))
            File.Delete(packagePath);

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(pluginPath, $"{projectName}.plugin");
        archive.CreateEntryFromFile(manifestPath, "manifest.json");
        archive.CreateEntryFromFile(checksumPath, $"{projectName}.plugin.sha256");
    }

    private void CleanupTempFiles(string tempRoot)
    {
        try
        {
            Directory.Delete(tempRoot, recursive: true);
            LogInfo("Cleaned up intermediate files.");
        }
        catch (Exception ex)
        {
            LogError($"Failed to cleanup temporary files: {ex.Message}");
        }
    }

    private bool RunCommand(string fileName, string arguments)
    {
        LogInfo($"▶ Running: {fileName} {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            LogError("Failed to start process.");
            return false;
        }

        process.OutputDataReceived += (_, e) => { if (e.Data != null && _options.Verbose) LogInfo(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) LogError(e.Data); };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            LogError($"Command failed with exit code {process.ExitCode}");
            return false;
        }

        return true;
    }

    private string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private void LogInfo(string message)
    {
        if (_options.Verbose)
            Console.WriteLine(message);
    }

    private void LogError(string message)
    {
        if (_options.Verbose)
            Console.Error.WriteLine(message);
    }
}