using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace MXFInspect.Avalonia.Views;

/// <summary>
/// Multi-value variant: [0] = byte[], [1] = base offset (long). Lets the hex
/// pane bind both the data and its file offset (ConverterParameter cannot bind).
/// </summary>
public sealed class HexDumpMultiConverter : IMultiValueConverter
{
    public static readonly HexDumpMultiConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var bytes = values.Count > 0 ? values[0] as byte[] : null;
        long baseOffset = 0;
        if (values.Count > 1 && values[1] is long l) baseOffset = l;
        return HexDumpConverter.Instance.Convert(bytes, targetType, baseOffset, culture);
    }
}

/// <summary>
/// Formats a byte array as a classic hex dump (offset | hex bytes | ASCII).
/// Used by the hex viewer pane. ConverterParameter carries the base offset.
/// </summary>
public sealed class HexDumpConverter : IValueConverter
{
    public static readonly HexDumpConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return string.Empty;

        long baseOffset = 0;
        if (parameter is long l) baseOffset = l;
        else if (parameter is IConvertible c) baseOffset = c.ToInt64(CultureInfo.InvariantCulture);

        var sb = new StringBuilder(bytes.Length * 4);
        const int perLine = 16;
        for (int i = 0; i < bytes.Length; i += perLine)
        {
            sb.Append((baseOffset + i).ToString("X8")).Append("  ");

            for (int j = 0; j < perLine; j++)
            {
                if (i + j < bytes.Length)
                    sb.Append(bytes[i + j].ToString("X2")).Append(' ');
                else
                    sb.Append("   ");
                if (j == 7) sb.Append(' ');
            }

            sb.Append(' ');
            for (int j = 0; j < perLine && i + j < bytes.Length; j++)
            {
                byte b = bytes[i + j];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
