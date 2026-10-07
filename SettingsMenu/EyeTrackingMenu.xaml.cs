using System.Diagnostics;

#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace OverjoyedVersion3;

public partial class EyeTrackingMenu : ContentView
{
#if WINDOWS
    private bool _active;
    private const string Host = "eye-tracking.overjoyed.invalid";
    private const string TrackerUrl = $"https://{Host}/index.html";
    private CoreWebView2? _core;
    private bool _initializing;
    private bool _cameraRequestPending;
#endif

    public EyeTrackingMenu()
    {
        InitializeComponent();
#if WINDOWS
        TrackerWebView.Loaded += OnWebViewReady;
        TrackerWebView.HandlerChanged += OnWebViewReady;
        TrackerWebView.Unloaded += (_, _) => Deactivate();
#else
        TrackerWebView.IsVisible = false;
        ReloadButton.IsVisible = false;
        StatusLabel.Text = "Local webcam eye tracking is currently supported on Windows only.";
#endif
    }

    public void Activate()
    {
#if WINDOWS
        _active = true;
        OnWebViewReady(this, EventArgs.Empty);
#endif
    }

    private void OnReloadClicked(object? sender, EventArgs e)
    {
#if WINDOWS
        _cameraRequestPending = false;
        OnWebViewReady(sender, e);
#endif
    }

    public void Deactivate()
    {
#if WINDOWS
        _active = false;
        _cameraRequestPending = false;
        // Navigating away destroys the document, including pending camera requests.
        try
        {
            _core?.Navigate("about:blank");
        }
        catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            ReportError("Unable to close the eye-tracking browser", error);
        }
#endif
    }

#if WINDOWS
    private async void OnWebViewReady(object? sender, EventArgs e)
    {
        if (!_active || _initializing ||
            TrackerWebView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 native)
            return;

        _initializing = true;
        try
        {
            await native.EnsureCoreWebView2Async();
            if (_core != native.CoreWebView2)
            {
                DetachCore();
                _core = native.CoreWebView2;
                var directory = await EyeTrackingAssets.ExtractAsync();
                _core.SetVirtualHostNameToFolderMapping(Host, directory, CoreWebView2HostResourceAccessKind.DenyCors);
                _core.PermissionRequested += OnPermissionRequested;
                _core.WebMessageReceived += OnWebMessageReceived;
                _core.NavigationStarting += OnNavigationStarting;
                _core.NavigationCompleted += OnNavigationCompleted;
                _core.NewWindowRequested += OnNewWindowRequested;
                _core.ProcessFailed += OnProcessFailed;
                _core.Settings.AreDefaultContextMenusEnabled = false;
            }

            if (_active)
            {
                StatusLabel.Text = "Local tracking only. Camera access starts when you select Start Camera.";
                _core.Navigate(TrackerUrl);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Runtime.InteropServices.COMException or InvalidOperationException or
            System.Text.Json.JsonException or NotSupportedException)
        {
            ReportError("Unable to load the local eye tracker", error);
            DetachCore();
        }
        finally
        {
            _initializing = false;
        }
    }

    private void DetachCore()
    {
        if (_core == null) return;
        _core.PermissionRequested -= OnPermissionRequested;
        _core.WebMessageReceived -= OnWebMessageReceived;
        _core.NavigationStarting -= OnNavigationStarting;
        _core.NavigationCompleted -= OnNavigationCompleted;
        _core.NewWindowRequested -= OnNewWindowRequested;
        _core.ProcessFailed -= OnProcessFailed;
        _core = null;
        _cameraRequestPending = false;
    }

    private static bool IsTrusted(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
        parsed.Scheme == Uri.UriSchemeHttps && parsed.Host == Host && parsed.IsDefaultPort;

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!_active || !IsTrusted(e.Source)) return;
        var message = e.TryGetWebMessageAsString();
        const string prefix = "request-camera:";
        if (message.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(message[prefix.Length..], out var session) && session > 0)
        {
            _cameraRequestPending = true;
            _core?.PostWebMessageAsString($"camera-authorized:{session}");
        }
        else if (message == "camera-stopped") _cameraRequestPending = false;
    }

    private void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        var allow = _active && _cameraRequestPending && IsTrusted(e.Uri) &&
            e.PermissionKind == CoreWebView2PermissionKind.Camera;
        e.State = allow ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
        // Persist the trusted origin's grant so enumeration exposes all cameras even after capture stops.
        e.SavesInProfile = allow;
        e.Handled = true;
        if (e.PermissionKind == CoreWebView2PermissionKind.Camera)
            _cameraRequestPending = false;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (e.Uri == "about:blank") return;
        if (!_active || !IsTrusted(e.Uri))
        {
            e.Cancel = true;
            StatusLabel.Text = "Navigation outside the bundled eye-tracking page was blocked.";
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_active && !e.IsSuccess)
            ReportError("Unable to display the local eye tracker",
                new IOException($"WebView2 navigation failed: {e.WebErrorStatus}."));
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        StatusLabel.Text = "Opening additional browser windows from the tracker is disabled.";
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _cameraRequestPending = false;
        ReportError("The eye-tracking browser stopped",
            new InvalidOperationException($"WebView2 process failure: {e.ProcessFailedKind}. Reopen Eye Tracking to retry."));
    }
#endif

    private void ReportError(string message, Exception error)
    {
        Trace.TraceError($"{message}: {error}");
        StatusLabel.Text = $"{message}: {error.Message}";
    }
}
