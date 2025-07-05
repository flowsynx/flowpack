# FlowPack

FlowPack is a .NET packaging utility for building, packaging, and distributing FlowSynx's plugins. It takes a .NET project, compiles it, generates metadata, and bundles it into a distributable `.fspack` file.

## Features

- Build and package a .csproj plugin into a .fspack archive.
- Extracts plugin metadata automatically.
- Generates SHA256 checksums.
- Produces `.fspack` files for easy distribution.
- Supports clean builds and verbose output.
- Includes a plugin loader for dynamic plugin loading/unloading.
- Simple CLI

---

## 📦 Download

Download the latest version of FlowPack from the [Release page](https://github.com/flowsynx/flowpack/releases) and extract it.

---

## 🚀 Usage

### Command Line Interface (CLI)

```bash
flowpack [options] <project.csproj>
```

### Options

| Option            | Description                                                               |
| ----------------- | ------------------------------------------------------------------------- |
| `--output <path>` | Specify the output `.fspack` file path. Defaults to `./<project>.fspack`. |
| `--clean`         | Perform a clean build before packaging.                                   |
| `--verbose`       | Enable verbose logging.                                                   |
| `--help`, `-h`    | Show help information.                                                    |
| `--version`, `-v` | Display the version of FlowPack.                                          |

### Example

```bash
flowpack --output MyPlugin.fspack --clean --verbose ./src/MyPlugin/MyPlugin.csproj
```

This will:

1. Clean and build the `MyPlugin.csproj` in Release mode.
2. Publish the plugin.
3. Package it as `MyPlugin.fspack`.

---

## 📂 Package Structure

A `.fspack` file is a ZIP archive containing:

- `<PluginName>.plugin` - The compiled plugin DLLs and dependencies.
- `manifest.json` - Metadata about the plugin.
- `<PluginName>.plugin.sha256` - SHA256 checksum of the plugin file.

---

## 📝 Plugin Metadata Example

The `manifest.json` file contains metadata like:

```json
{
  "Id": "b2f5ff47-2fc6-4bdb-8c73-9d69f4e1f94d",
  "Type": "DataProcessor",
  "Version": "1.0.0",
  "CompanyName": "YourCompany",
  "Description": "A sample plugin for processing data.",
  "License": "MIT",
  "LicenseUrl": "https://opensource.org/licenses/MIT",
  "Authors": ["Jane Doe"],
  "Tags": ["data", "processor", "plugin"],
  "CategoryId": "Communication"
}
```

---

## 🔗 Related Projects

- [FlowSynx.PluginCore](https://github.com/flowsynx/plugin-core): Core interfaces for FlowSynx plugin systems.

---

## 📜 License

This project is licensed under the MIT License. See LICENSE for details.

---

## 👨‍💻 Contributing

Contributions are welcome! Please open an issue or submit a pull request.

---

## 🆘 Support

For questions or issues, please create an issue on the [GitHub repository](https://github.com/flowsynx/flowpack/issues).