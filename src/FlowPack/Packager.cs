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
        string projectPath = _options.ProjectPath;

        if (!File.Exists(projectPath) || Path.GetExtension(projectPath) != ".csproj")
            throw new FileNotFoundException("Provided file must be a valid .csproj file.");

        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        string configuration = "Release";
        string tempOutput = Path.Combine(Path.GetTempPath(), "publish_output_" + Guid.NewGuid());
        Directory.CreateDirectory(tempOutput);

        if (_options.Clean)
        {
            RunCommand("dotnet", $"clean \"{projectPath}\" -c {configuration}");
        }

        if (!RunCommand("dotnet", $"build \"{projectPath}\" -c {configuration}"))
            return false;

        if (!RunCommand("dotnet", $"publish \"{projectPath}\" -c {configuration} -o \"{tempOutput}\""))
            return false;

        // Create .plugin file from published output
        string pluginTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".plugin");
        string pluginMetadataTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        var metadata = PluginReflector.ExtractPluginMetadata(tempOutput);
        if (metadata == null)
        {
            Console.WriteLine("No valid plugin metadata found.");
            return false;
        }

        var manifestPath = PluginReflector.SaveMetadataToFile(metadata, tempOutput);

        ZipFile.CreateFromDirectory(tempOutput, pluginTempPath);
        LogInfo($"Plugin created: {pluginTempPath}");

        // Compute SHA256
        string checksum = ComputeSha256(pluginTempPath);
        string checksumPath = pluginTempPath + ".sha256";
        File.WriteAllText(checksumPath, checksum);
        LogInfo($"SHA256: {checksum}");

        // Determine final .fspack path
        var fspackPath = _options.OutputPath;
        if (string.IsNullOrWhiteSpace(fspackPath))
        {
            fspackPath = Path.Combine(Directory.GetCurrentDirectory(), $"{projectName}.fspack");
        }
        else if (!fspackPath.EndsWith(".fspack", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Output file must have a .fspack extension.");
        }

        if (File.Exists(fspackPath)) File.Delete(fspackPath);
        using (var archive = ZipFile.Open(fspackPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(pluginTempPath, $"{projectName}.plugin");
            archive.CreateEntryFromFile(manifestPath, "manifest.json");
            archive.CreateEntryFromFile(checksumPath, $"{projectName}.plugin.sha256");
        }

        LogInfo($"Final package created: {fspackPath}");

        // Cleanup
        File.Delete(pluginTempPath);
        File.Delete(checksumPath);
        LogInfo("Cleaned up intermediate files.");

        return true;
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
        {
            Console.WriteLine(message);
        }
    }

    private void LogError(string message)
    {
        if (_options.Verbose)
        {
            Console.Error.WriteLine(message);
        }
    }
}