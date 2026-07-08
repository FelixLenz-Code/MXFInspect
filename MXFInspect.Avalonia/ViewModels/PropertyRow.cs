using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MXFInspect.Avalonia.ViewModels;

/// <summary>One row in the read-only property grid.</summary>
public sealed class PropertyRow
{
    public string Category { get; init; } = "Misc";
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Builds property rows from an object using the same rules as the original
    /// ReadOnlyPropertyGrid: reflect all browsable public properties, honouring
    /// [Category] and [Description] metadata.
    /// </summary>
    public static IReadOnlyList<PropertyRow> FromObject(object? obj)
    {
        if (obj == null)
            return System.Array.Empty<PropertyRow>();

        var rows = new List<PropertyRow>();
        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(obj))
        {
            if (!prop.IsBrowsable)
                continue;

            string value;
            try
            {
                value = prop.GetValue(obj)?.ToString() ?? string.Empty;
            }
            catch
            {
                value = "<error>";
            }

            rows.Add(new PropertyRow
            {
                Category = string.IsNullOrEmpty(prop.Category) ? "Misc" : prop.Category,
                Name = prop.DisplayName,
                Value = value,
            });
        }

        return rows
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .ToList();
    }
}
