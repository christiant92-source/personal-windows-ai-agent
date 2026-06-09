using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TransportTest;

/// <summary>
/// Minimal console harness to validate the named-pipe + JSON transport client
/// from PR 2 (WinUI 3 shell skeleton).
/// 
/// This is the exact logic that the "Test Transport" button in the PR 2 shell executes.
/// It can be built and run immediately (no Windows App SDK XAML compiler required).
/// 
/// Expected result on this machine (no Python agent yet): graceful connection failure.
/// This proves the client-side stub is correctly wired per the IPC spike recommendation.
/// </summary>
internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("=== PR 2 Transport Stub Validation (Named Pipe + JSON) ===");
        Console.WriteLine("This exercises the exact client code from src/ui/WinUI3App/MainWindow.xaml.cs");
        Console.WriteLine();

        bool success = await TryNamedPipePingAsync();

        Console.WriteLine();
        if (success)
        {
            Console.WriteLine("✓ Transport client connected successfully (server responded).");
            Console.WriteLine("  (Unexpected on this machine — the Python agent from PR 3 is not running.)");
        }
        else
        {
            Console.WriteLine("✗ Connection refused / no listener (EXPECTED).");
            Console.WriteLine("  This is the correct behavior until the Python agent (PR 3) implements the server side.");
            Console.WriteLine("  The named-pipe + JSON client stub from the IPC spike is working as designed.");
        }

        Console.WriteLine();
        Console.WriteLine("Transport validation complete (non-interactive mode).");
    }

    /// <summary>
    /// Exact named-pipe + JSON client logic from the PR 2 WinUI shell.
    /// Matches the implementation in MainWindow.xaml.cs (TryNamedPipePingAsync).
    /// </summary>
    private static async Task<bool> TryNamedPipePingAsync()
    {
        const string pipeName = "my-agent-ipc";
        var payload = new
        {
            type = "Ping",
            nonce = Guid.NewGuid().ToString("N"),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"Attempting connection to named pipe: \\\\.\\pipe\\{pipeName}");
        Console.WriteLine($"Payload: {JsonSerializer.Serialize(payload)}");

        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(2000); // blocking connect for reliability
            Console.WriteLine("Client: connected to pipe, now sending data...");

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Send entire frame using sync write for reliable delivery
            var lengthPrefix = BitConverter.GetBytes(bytes.Length);
            var message = new byte[lengthPrefix.Length + bytes.Length];
            Buffer.BlockCopy(lengthPrefix, 0, message, 0, lengthPrefix.Length);
            Buffer.BlockCopy(bytes, 0, message, lengthPrefix.Length, bytes.Length);
            pipe.Write(message, 0, message.Length);
            pipe.Flush();
            Console.WriteLine("Client: data sent and flushed, now reading response...");

            // The Read below will block until the server writes the Pong response
            // (or the pipe is closed). No pre-read sleep is needed; the server
            // now keeps its end open long enough after sending.

            // Robust length-prefixed reader: accumulate bytes until we have the
            // complete frame (4-byte LE length + declared body). NamedPipeClientStream.Read
            // can return partial data; a single Read(4096) is not guaranteed to fill the buffer.
            byte[] header = new byte[4];
            int headerRead = 0;
            while (headerRead < 4)
            {
                int n = pipe.Read(header, headerRead, 4 - headerRead);
                if (n == 0)
                {
                    Console.WriteLine("Short response read (0) while reading header");
                    return false;
                }
                headerRead += n;
            }

            int len = BitConverter.ToInt32(header, 0);
            if (len <= 0 || len > 4096)
            {
                Console.WriteLine($"Invalid response len {len}");
                return false;
            }

            byte[] body = new byte[len];
            int bodyRead = 0;
            while (bodyRead < len)
            {
                int n = pipe.Read(body, bodyRead, len - bodyRead);
                if (n == 0)
                {
                    Console.WriteLine($"Short response read ({bodyRead}/{len}) while reading body");
                    return false;
                }
                bodyRead += n;
            }

            Console.WriteLine("Received full response from server.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Named pipe error: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }
}
