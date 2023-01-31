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
        CredentialManager = new OSX.OSXCredentialManager();
#else
        throw new System.PlatformNotSupportedException();
#endif
    }

    // ReSharper disable MemberInitializerValueIgnored
    // ReSharper disable ReplaceAutoPropertyWithComputedProperty
    internal static IOSFunctionality OSFunctionality { get; } = default!;
    internal static ICredentialManager CredentialManager { get; } = default!;
    internal static IWebView? WebView { get; } = default;
}