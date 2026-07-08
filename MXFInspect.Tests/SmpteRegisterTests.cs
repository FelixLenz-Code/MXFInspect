using System.Linq;
using Myriadbits.MXF.Identifiers;
using Xunit;

namespace MXFInspect.Tests;

/// <summary>
/// Verifies that the embedded SMPTE registry files (previously bound through a
/// WinForms/System.Drawing resx) are readable at runtime on this platform and
/// parse into the expected dictionaries. This is the core cross-platform
/// regression guard for the parser port.
/// </summary>
public class SmpteRegisterTests
{
    [Fact]
    public void SymbolDictionary_LoadsManyKeysFromEmbeddedRegisters()
    {
        var keys = SymbolDictionary.GetKeys();
        Assert.NotNull(keys);
        // The SMPTE Labels/Elements/Groups registers contain thousands of entries.
        Assert.True(keys.Count > 500, $"Expected >500 symbols, got {keys.Count}");
    }

    [Fact]
    public void KeyDictionary_LoadsManyKeysFromEmbeddedRegisters()
    {
        var keys = KeyDictionary.GetKeys();
        Assert.NotNull(keys);
        Assert.True(keys.Count > 500, $"Expected >500 keys, got {keys.Count}");
    }

    [Fact]
    public void SymbolDictionary_ContainsResolvableNamedEntries()
    {
        var keys = SymbolDictionary.GetKeys();
        // Keys of this dictionary are the human-readable SMPTE symbol names.
        Assert.Contains(keys.Keys, name => !string.IsNullOrWhiteSpace(name));
    }
}
