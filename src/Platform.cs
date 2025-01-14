namespace ConnectToUrl;

#pragma warning disable CA1416 // Validate platform compatibility

internal static class Platform {
    static Platform() {
#if WINDOWS
        OSFunctionality = new Windows.WindowsFunctionality();
        CredentialManager = new Windows.WindowsCredentialManager();

#if WEBVIEW
        WebView = new Windows.WindowsWebView();
#endif

#elif MACOS
        OSFunctionality = new OSX.OSXFunctionality();

#elif LINUX
        OSFunctionality = new Linux.LinuxFunctionality();
        CredentialManager = null;
        CredentialStore = new Linux.LinuxCredentialStore();

#else
        throw new System.PlatformNotSupportedException();
#endif
    }

    // ReSharper disable RedundantDefaultMemberInitializer
    internal static IOSFunctionality OSFunctionality { get; }
    internal static ICredentialManager? CredentialManager { get; } = null;
    internal static ICredentialStore? CredentialStore { get; } = null;
    internal static IWebView? WebView { get; } = null;
}