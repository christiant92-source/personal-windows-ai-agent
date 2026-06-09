using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WinUI3App;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void ShowPane(VerticalStackLayout pane)
    {
        ChatPane.IsVisible = false;
        SuggestionsPane.IsVisible = false;
        DashboardPane.IsVisible = false;
        SettingsPane.IsVisible = false;

        pane.IsVisible = true;
    }

    private void NavChat_Click(object sender, EventArgs e) => ShowPane(ChatPane);
    private void NavSuggestions_Click(object sender, EventArgs e) => ShowPane(SuggestionsPane);
    private void NavDashboard_Click(object sender, EventArgs e) => ShowPane(DashboardPane);
    private void NavSettings_Click(object sender, EventArgs e) => ShowPane(SettingsPane);

    private async void SendChat_Click(object sender, EventArgs e) => await SendChatAsync();
    private async void ChatInput_Completed(object sender, EventArgs e) => await SendChatAsync();

    private async Task SendChatAsync()
    {
        string text = ChatInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;

        ChatLog.Text += $"> {text}\n";
        ChatInput.Text = "";

        var (ok, responseText, route, error) = await TryNamedPipeChatAsync(text);
        if (ok && !string.IsNullOrEmpty(responseText))
        {
            string routeTag = string.IsNullOrEmpty(route) ? "" : $"[{route}] ";
            ChatLog.Text += $"< Agent {routeTag}: {responseText}\n\n";
        }
        else
        {
            ChatLog.Text += $"< Agent (error): {error ?? "unknown"}\n\n";
        }
    }

    private async void TestTransport_Click(object sender, EventArgs e)
    {
        TransportStatus.Text = "Connecting via named pipe (stub per ipc-spike.md recommendation)...";
        var (ok, info) = await TryNamedPipePingAsync();
        if (ok)
        {
            string noncePart = string.IsNullOrEmpty(info) ? "Nonce echoed." : $"Nonce: {info} echoed.";
            TransportStatus.Text = $"✓ Connected! Server responded (PR 3 Python agent). {noncePart}";
        }
        else
        {
            TransportStatus.Text = $"Connection refused / no listener (expected until Python agent server is running). Error: {info ?? "unknown"}. Named pipe transport stub is wired per ipc-spike.md.";
        }
    }

    /// <summary>
    /// Minimal named-pipe + JSON client stub matching the IPC spike recommendation.
    /// Sends a Ping and expects a Pong from the PR 3 Python agent server.
    /// </summary>
    private async Task<(bool success, string error)> TryNamedPipePingAsync()
    {
        const string pipeName = "my-agent-ipc";
        var payload = new { type = "Ping", nonce = Guid.NewGuid().ToString("N"), ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(2000); // blocking connect for reliability with local pipe
            System.Diagnostics.Debug.WriteLine("Client: connected to pipe, now sending data...");

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Send entire frame (length + payload) using sync write for reliable delivery on named pipe
            var lengthPrefix = BitConverter.GetBytes(bytes.Length);
            var message = new byte[lengthPrefix.Length + bytes.Length];
            Buffer.BlockCopy(lengthPrefix, 0, message, 0, lengthPrefix.Length);
            Buffer.BlockCopy(bytes, 0, message, lengthPrefix.Length, bytes.Length);
            pipe.Write(message, 0, message.Length);
            pipe.Flush();
            System.Diagnostics.Debug.WriteLine("Client: data sent and flushed, now reading response...");

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
                    string msg = "short response read (0) while reading header";
                    System.Diagnostics.Debug.WriteLine($"Named pipe: {msg}");
                    return (false, msg);
                }
                headerRead += n;
            }

            int len = BitConverter.ToInt32(header, 0);
            if (len <= 0 || len > 4096)
            {
                string msg = $"invalid response len {len}";
                System.Diagnostics.Debug.WriteLine($"Named pipe: {msg}");
                return (false, msg);
            }

            byte[] body = new byte[len];
            int bodyRead = 0;
            while (bodyRead < len)
            {
                int n = pipe.Read(body, bodyRead, len - bodyRead);
                if (n == 0)
                {
                    string msg = $"short response read ({bodyRead}/{len}) while reading body";
                    System.Diagnostics.Debug.WriteLine($"Named pipe: {msg}");
                    return (false, msg);
                }
                bodyRead += n;
            }

            // We now have the complete Pong frame. Parse the echoed nonce for the UI banner.
            string echoedNonce = "";
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("nonce", out var n))
                    echoedNonce = n.GetString() ?? "";
            }
            catch { /* best effort for display */ }

            System.Diagnostics.Debug.WriteLine($"Client: received Pong, nonce={echoedNonce}");
            return (true, echoedNonce);
        }
        catch (Exception ex)
        {
            string msg = $"{ex.GetType().Name} - {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Named pipe error after connect: {msg}");
            return (false, msg);
        }
    }

    /// <summary>
    /// Sends a chat message over the named pipe to the PR 3 Python agent.
    /// Expects a ChatResponse with the agent's reply text.
    /// </summary>
    private async Task<(bool success, string? responseText, string? route, string? error)> TryNamedPipeChatAsync(string text)
    {
        const string pipeName = "my-agent-ipc";
        var payload = new
        {
            type = "Chat",
            text = text,
            nonce = Guid.NewGuid().ToString("N"),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(2000);
            System.Diagnostics.Debug.WriteLine("Client: connected for chat, now sending...");

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            var lengthPrefix = BitConverter.GetBytes(bytes.Length);
            var message = new byte[lengthPrefix.Length + bytes.Length];
            Buffer.BlockCopy(lengthPrefix, 0, message, 0, lengthPrefix.Length);
            Buffer.BlockCopy(bytes, 0, message, lengthPrefix.Length, bytes.Length);
            pipe.Write(message, 0, message.Length);
            pipe.Flush();
            System.Diagnostics.Debug.WriteLine("Client: chat sent, now reading response...");

            // Robust length-prefixed read (same pattern as ping response).
            byte[] header = new byte[4];
            int headerRead = 0;
            while (headerRead < 4)
            {
                int n = pipe.Read(header, headerRead, 4 - headerRead);
                if (n == 0)
                {
                    return (false, null, null, "short header while reading chat response");
                }
                headerRead += n;
            }

            int len = BitConverter.ToInt32(header, 0);
            if (len <= 0 || len > 4096)
            {
                return (false, null, null, $"invalid chat response len {len}");
            }

            byte[] body = new byte[len];
            int bodyRead = 0;
            while (bodyRead < len)
            {
                int n = pipe.Read(body, bodyRead, len - bodyRead);
                if (n == 0)
                {
                    return (false, null, null, $"short body while reading chat response ({bodyRead}/{len})");
                }
                bodyRead += n;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            string respType = root.TryGetProperty("type", out var t) ? (t.GetString() ?? "") : "";

            if (respType == "ChatResponse")
            {
                string respText = root.TryGetProperty("text", out var txt) ? (txt.GetString() ?? "") : "";
                string route = root.TryGetProperty("route", out var r) ? (r.GetString() ?? "") : "";
                System.Diagnostics.Debug.WriteLine($"Client: received ChatResponse route={route}");
                return (true, respText, route, null);
            }
            else
            {
                return (false, null, null, $"unexpected response type: {respType}");
            }
        }
        catch (Exception ex)
        {
            string msg = $"{ex.GetType().Name} - {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Named pipe chat error: {msg}");
            return (false, null, null, msg);
        }
    }
}
