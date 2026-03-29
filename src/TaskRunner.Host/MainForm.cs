using Microsoft.Web.WebView2.WinForms;

namespace TaskRunner.Host;

public class MainForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _url;

    public MainForm(string url)
    {
        _url = url;
        
        Text = "Task Runner";
        Width = 1280;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_webView);

        // Ensure WebView2 reflows when the form resizes
        Resize += (s, e) => 
        {
            if (_webView != null) 
            {
                _webView.Size = ClientSize;
            }
        };

        Load += MainForm_Load;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.Navigate(_url);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _webView?.Dispose();
        base.OnFormClosing(e);
    }
}
