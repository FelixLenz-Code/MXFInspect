using System;
using System.IO;
using Avalonia;

namespace MXFInspect.Avalonia;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        // On Linux, broken/absent GPU drivers make Skia's GL backend crash right
        // after the window appears ("flashes in the taskbar then disappears").
        // Keep GPU as the default but fall back to software rendering, and let the
        // user force pure software rendering with MXFINSPECT_RENDER=software.
        if (OperatingSystem.IsLinux())
        {
            var forceSoftware = string.Equals(
                Environment.GetEnvironmentVariable("MXFINSPECT_RENDER"),
                "software", StringComparison.OrdinalIgnoreCase);

            builder = builder.With(new X11PlatformOptions
            {
                RenderingMode = forceSoftware
                    ? new[] { X11RenderingMode.Software }
                    : new[] { X11RenderingMode.Glx, X11RenderingMode.Egl, X11RenderingMode.Software },
            });
        }

        return builder;
    }

    private static void LogCrash(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MXFInspect");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:O}] Unhandled exception:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Never let logging itself take down the process.
        }
        Console.Error.WriteLine(ex);
    }
}
