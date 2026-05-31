using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WinUI3App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = "My Agent — PR 2 WinUI 3 Shell Skeleton";
    }

    private void ShowPane(FrameworkElement pane)
    {
        ChatPane.Visibility = Visibility.Collapsed;
        SuggestionsPane.Visibility = Visibility.Collapsed;
        DashboardPane.Visibility = Visibility.Collapsed;
        SettingsPane.Visibility = Visibility.Collapsed;

        pane.Visibility = Visibility.Visible;
    }

    private void NavChat_Click(object sender, RoutedEventArgs e) => ShowPane(ChatPane);
    private void NavSuggestions_Click(object sender, RoutedEventArgs e) => ShowPane(SuggestionsPane);
    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowPane(DashboardPane);
    private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowPane(SettingsPane);

    private async void SendChat_Click(object sender, RoutedEventArgs e) => await SendChatAsync();
    private async void ChatInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await SendChatAsync();
            e.Handled = true;
        }
    }

    private async Task SendChatAsync()
    {
        string text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        ChatLog.Text += $"> {text}\n";
        ChatInput.Text = "";

        // Stub response — in PR 3 this will go through the Python agent via transport
        await Task.Delay(120);
        ChatLog.Text += "< Agent (stub): This is a local shell-only response. Real routing + tools land in PR 3+.\n";
        ChatLog.Text += "< (IPC spike recommended named-pipe + JSON for this workload.)\n\n";
    }

    private async void TestTransport_Click(object sender, RoutedEventArgs e)
    {
        TransportStatus.Text = "Connecting via named pipe (stub per ipc-spike.md recommendation)...";
        bool ok = await TryNamedPipePingAsync();
        TransportStatus.Text = ok 
            ? "✓ Connected (stub). Server not present yet (PR 3). Round-trip simulated."
            : "✗ Connection refused / no listener (expected — Python agent lands in PR 3). Named pipe transport stub is wired.";
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

            // Read response length + body (stub — server not running)
            byte[] lenBuf = new byte[4];
            await pipe.ReadAsync(lenBuf);
            int len = BitConverter.ToInt32(lenBuf);
            byte[] resp = new byte[Math.Min(len, 4096)];
            await pipe.ReadAsync(resp);

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
