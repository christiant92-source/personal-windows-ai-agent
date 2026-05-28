using System;
using Windows.Foundation;

namespace WinRTConsoleSmoke;

/// <summary>
/// Minimal console + WinRT projection smoke test.
/// Verifies that the Windows App SDK / WinRT projection works when only
/// the .NET SDK + Windows App SDK workload are present (no full VS).
/// </summary>
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("WinRT Lite Smoke Test (PR 1)");
        Console.WriteLine($"Runtime: {Environment.OSVersion} | .NET {Environment.Version}");

        // Exercise basic WinRT projection (Windows.Foundation)
        var rect = new Rect(10, 20, 300, 150);
        Console.WriteLine($"WinRT Rect projection OK: X={rect.X}, Width={rect.Width}");

        var uri = new Uri("https://example.com/agent");
        Console.WriteLine($"WinRT Uri projection OK: {uri}");

        Console.WriteLine("SUCCESS: WinRT projection loaded without full Visual Studio.");
        Environment.ExitCode = 0;
    }
}
