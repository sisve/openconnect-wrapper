using System;

namespace ConnectToUrl;

/// <summary>
/// A credential manager that includes the user interface required to ask for
/// the credentials, including optionally persisting them.
/// </summary>
internal interface ICredentialManager {
    public IVpnCredentials? AskForCredentials(String url, String messageText);
    public IVpnCredentials? ForceAskForCredentials(String url, String messageText);
}