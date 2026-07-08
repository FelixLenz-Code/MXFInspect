using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Myriadbits.MXF;
using MXFInspect.Avalonia.Services;

namespace MXFInspect.Avalonia.ViewModels;

/// <summary>
/// A single row in either the physical (offset) tree or the logical tree.
/// Children are loaded lazily on first expansion so multi-gigabyte files with
/// huge object trees stay responsive.
/// </summary>
public partial class MxfNodeViewModel : ObservableObject
{
    private readonly bool _isLogical;
    private readonly SettingsService _settings;
    private readonly TreeFilter _filter;
    private bool _childrenLoaded;

    /// <summary>The underlying data object (used for properties, hex view and navigation).</summary>
    public MXFObject Object { get; }

    /// <summary>The logical wrapper when this node lives in the logical tree, otherwise null.</summary>
    public MXFLogicalObject? Logical { get; }

    public ObservableCollection<MxfNodeViewModel> Children { get; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public string Text { get; }
    public string OffsetText { get; }
    public IBrush? Foreground { get; }

    private MxfNodeViewModel(MXFObject obj, MXFLogicalObject? logical, bool isLogical,
        SettingsService settings, TreeFilter filter)
    {
        Object = obj;
        Logical = logical;
        _isLogical = isLogical;
        _settings = settings;
        _filter = filter;

        Text = (logical as object ?? obj).ToString() ?? string.Empty;
        OffsetText = settings.ShowOffsetAsHex ? $"0x{obj.Offset:X}" : obj.Offset.ToString();
        Foreground = TypeColors.BrushFor(obj.Type, settings);

        if (HasChildSource())
            Children.Add(Placeholder); // makes the expander appear before children are loaded
    }

    public static MxfNodeViewModel ForPhysical(MXFObject obj, SettingsService settings, TreeFilter filter)
        => new(obj, null, false, settings, filter);

    public static MxfNodeViewModel ForLogical(MXFLogicalObject logical, SettingsService settings, TreeFilter filter)
        => new(logical.Object, logical, true, settings, filter);

    private static readonly MxfNodeViewModel Placeholder = CreatePlaceholder();
    private static MxfNodeViewModel CreatePlaceholder()
        => new(new PlaceholderObject(), null, false, new SettingsService(), TreeFilter.None);

    private bool HasChildSource()
        => _isLogical ? Logical!.Children.Any() : ChildObjects().Any();

    private IEnumerable<MXFObject> ChildObjects()
        => Object.Children.Where(_filter.Matches);

    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
            EnsureChildrenLoaded();
    }

    public void EnsureChildrenLoaded()
    {
        if (_childrenLoaded)
            return;
        _childrenLoaded = true;
        Children.Clear();

        if (_isLogical)
        {
            foreach (var child in Logical!.Children)
                Children.Add(ForLogical(child, _settings, _filter));
        }
        else
        {
            foreach (var child in ChildObjects().OrderBy(o => o.Offset))
                Children.Add(ForPhysical(child, _settings, _filter));
        }
    }

    /// <summary>A throwaway object so the placeholder node has a valid data object.</summary>
    private sealed class PlaceholderObject : MXFObject
    {
        public PlaceholderObject() : base(0) { }
    }
}
