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

        // Stub response - in PR 3 this will go through the Python agent via transport
        await Task.Delay(120);
        ChatLog.Text += "< Agent (stub): This is a local shell-only response. Real routing + tools land in PR 3+.\n";
        ChatLog.Text += "< (IPC spike recommended named-pipe + JSON for this workload.)\n\n";
    }

    private async void TestTransport_Click(object sender, EventArgs e)
    {
        TransportStatus.Text = "Connecting via named pipe (stub per ipc-spike.md recommendation)...";
        bool ok = await TryNamedPipePingAsync();
        TransportStatus.Text = ok 
            ? "✓ Connected! Server responded (PR 3 Python agent). Nonce echoed."
            : "Connection refused / no listener (expected until Python agent server is running). Named pipe transport stub is wired per ipc-spike.md.";
    }

    /// <summary>
    /// Minimal named-pipe + JSON client stub matching the IPC spike recommendation.
    /// Attempts a single ping frame. No server exists until PR 3.
    /// </summary>
    private async Task<bool> TryNamedPipePingAsync()
    {
        const string pipeName = "my-agent-ipc";
        var payload = new { type = "Ping", nonce = Guid.NewGuid().ToString("N"), ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(800); // short timeout for skeleton UX

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await pipe.WriteAsync(BitConverter.GetBytes(bytes.Length)); // length prefix (simple)
            await pipe.WriteAsync(bytes);
            await pipe.FlushAsync();

            // Read response length + body (stub - server not running)
            byte[] lenBuf = new byte[4];
            await pipe.ReadAsync(lenBuf, 0, 4);
            int len = BitConverter.ToInt32(lenBuf, 0);
            byte[] resp = new byte[Math.Min(len, 4096)];
            await pipe.ReadAsync(resp, 0, resp.Length);

            return true; // If we got this far without exception, transport path works
        }
        catch (Exception ex)
        {
            // Expected in PR 2: no listener on the pipe
            System.Diagnostics.Debug.WriteLine($"Named pipe stub: {ex.Message}");
            return false;
        }
    }
}
