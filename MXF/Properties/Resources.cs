using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;

namespace Myriadbits.MXF.Properties
{
    /// <summary>
    /// Cross-platform replacement for the former WinForms/System.Drawing based
    /// Resources.resx designer class. The SMPTE registry files are embedded into
    /// the assembly (see MXF.csproj) and read here as plain strings, so the parser
    /// has no dependency on System.Drawing or System.Windows.Forms and runs on
    /// Linux, Windows and macOS alike.
    /// </summary>
    internal static class Resources
    {
        private static readonly Assembly ThisAssembly = typeof(Resources).Assembly;
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>();

        internal static string ANC_Identifiers => Load("Myriadbits.MXF.SMPTE.ANC_Identifiers.csv");
        internal static string Elements => Load("Myriadbits.MXF.SMPTE.Elements.xml");
        internal static string Groups => Load("Myriadbits.MXF.SMPTE.Groups.xml");
        internal static string Labels => Load("Myriadbits.MXF.SMPTE.Labels.xml");
        internal static string Types => Load("Myriadbits.MXF.SMPTE.Types.xml");

        private static string Load(string resourceName)
        {
            return Cache.GetOrAdd(resourceName, name =>
            {
                using (Stream stream = ThisAssembly.GetManifestResourceStream(name))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException(
                            $"Embedded SMPTE resource '{name}' was not found. " +
                            "Check the EmbeddedResource LogicalName entries in MXF.csproj.");
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            });
        }
    }
}
