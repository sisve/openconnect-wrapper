using System;
using System.Runtime.Versioning;

namespace ConnectToUrl.OSX; 

[SupportedOSPlatform("OSX")]
internal class OSXCredentialManager : ICredentialManager {
    public IVpnCredentials? AskForCredentials(String url, String messageText) {
        return AskForCredentials(url, messageText, force: false);
    }

    public IVpnCredentials? ForceAskForCredentials(String url, String messageText) {
        return AskForCredentials(url, messageText, force: true);
    }
    
    private static IVpnCredentials? AskForCredentials(String url, String messageText, Boolean force) {
        throw new NotSupportedException();
    }
}