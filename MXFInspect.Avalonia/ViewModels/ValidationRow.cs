using Avalonia.Media;
using Myriadbits.MXF;

namespace MXFInspect.Avalonia.ViewModels;

/// <summary>One line of the conformance/validation report.</summary>
public sealed class ValidationRow
{
    public string Category { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public IBrush Brush { get; init; } = Brushes.Gray;

    public static IBrush BrushFor(MXFValidationState state) => state switch
    {
        MXFValidationState.Success => Brushes.SeaGreen,
        MXFValidationState.Warning => Brushes.DarkOrange,
        MXFValidationState.Error => Brushes.IndianRed,
        MXFValidationState.Info => Brushes.SteelBlue,
        MXFValidationState.Question => Brushes.MediumPurple,
        _ => Brushes.Gray,
    };
}
