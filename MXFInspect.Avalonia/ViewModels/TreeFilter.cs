using Myriadbits.MXF;

namespace MXFInspect.Avalonia.ViewModels;

/// <summary>
/// Filter applied while building the physical tree: optionally hides filler
/// objects and/or restricts nodes to those whose display text contains a
/// case-insensitive search term (mirrors the WinForms type/filler filters).
/// </summary>
public sealed class TreeFilter
{
    public static readonly TreeFilter None = new(false, null);

    public bool ExcludeFiller { get; }
    public string? TypeContains { get; }

    public TreeFilter(bool excludeFiller, string? typeContains)
    {
        ExcludeFiller = excludeFiller;
        TypeContains = string.IsNullOrWhiteSpace(typeContains) ? null : typeContains.Trim();
    }

    public bool Matches(MXFObject obj)
    {
        if (ExcludeFiller && obj.Type == MXFObjectType.Filler)
            return false;

        if (TypeContains != null)
        {
            var text = obj.ToString() ?? string.Empty;
            var typeName = obj.GetType().Name;
            if (text.IndexOf(TypeContains, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                typeName.IndexOf(TypeContains, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                // Keep the node if any descendant matches, so the tree stays navigable.
                foreach (var child in obj.Descendants())
                {
                    var ct = child.ToString() ?? string.Empty;
                    if (ct.IndexOf(TypeContains, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        child.GetType().Name.IndexOf(TypeContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            }
        }

        return true;
    }
}
