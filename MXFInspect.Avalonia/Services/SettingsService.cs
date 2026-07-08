using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using Myriadbits.MXF;

namespace MXFInspect.Avalonia.Services;

/// <summary>
/// Cross-platform, JSON-backed application settings. The file lives in the
/// per-user config directory (%APPDATA% on Windows, ~/.config on Linux,
/// ~/Library/Application Support on macOS) so it works on every platform.
/// </summary>
public class SettingsService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MXFInspect");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "settings.json");

    public bool ShowOffsetAsHex { get; set; } = true;
    public bool ExcludeFiller { get; set; }

    /// <summary>Colour overrides per object type, stored as hex strings.</summary>
    [JsonInclude]
    public Dictionary<MXFObjectType, Color> Colors { get; private set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new ColorJsonConverter() },
    };

    public static SettingsService Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var loaded = JsonSerializer.Deserialize<SettingsService>(json, JsonOptions);
                if (loaded != null)
                    return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable settings should never prevent the app from starting.
        }
        return new SettingsService();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Best-effort persistence.
        }
    }
}

internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Color.Parse(reader.GetString() ?? "#000000");

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
