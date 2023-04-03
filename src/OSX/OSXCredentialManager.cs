using System;

namespace ConnectToUrl.OSX;

internal class OSXCredentialManager : ICredentialManager {
    public IVpnCredentials? AskForCredentials(String url, String messageText) {
        return AskForCredentials(url, messageText, false);
    }

    public IVpnCredentials? ForceAskForCredentials(String url, String messageText) {
        return AskForCredentials(url, messageText, true);
    }

    private static IVpnCredentials? AskForCredentials(String url, String messageText, Boolean force) {
        throw new NotSupportedException();
    }
}