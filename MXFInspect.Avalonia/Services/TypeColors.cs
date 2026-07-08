using System.Collections.Generic;
using Avalonia.Media;
using Myriadbits.MXF;

namespace MXFInspect.Avalonia.Services;

/// <summary>
/// Maps an <see cref="MXFObjectType"/> to the brush used to render its tree node,
/// mirroring the syntax colouring of the original WinForms application. The default
/// palette is overridable through <see cref="SettingsService"/>.
/// </summary>
public static class TypeColors
{
    public static readonly Dictionary<MXFObjectType, Color> Defaults = new()
    {
        [MXFObjectType.Partition] = Color.Parse("#1565C0"), // blue
        [MXFObjectType.Essence] = Color.Parse("#2E7D32"),   // green
        [MXFObjectType.Index] = Color.Parse("#6A1B9A"),     // purple
        [MXFObjectType.SystemItem] = Color.Parse("#00838F"),// teal
        [MXFObjectType.RIP] = Color.Parse("#AD1457"),       // pink
        [MXFObjectType.Meta] = Color.Parse("#EF6C00"),      // orange
        [MXFObjectType.Filler] = Color.Parse("#9E9E9E"),    // gray
        [MXFObjectType.Special] = Color.Parse("#C62828"),   // red
        [MXFObjectType.Normal] = Colors.Transparent,        // use theme foreground
    };

    private static readonly Dictionary<MXFObjectType, IBrush> BrushCache = new();

    public static IBrush? BrushFor(MXFObjectType type, SettingsService settings)
    {
        if (settings.Colors.TryGetValue(type, out var overridden))
            return new SolidColorBrush(overridden);

        if (type == MXFObjectType.Normal)
            return null; // let the control template pick the theme foreground

        if (!BrushCache.TryGetValue(type, out var brush))
        {
            brush = new SolidColorBrush(Defaults[type]);
            BrushCache[type] = brush;
        }
        return brush;
    }
}
