using System;

namespace ConnectToUrl;

/// <summary>
/// A credential manager that acts solely as a store,
/// with read/write capability, but without any user interface.
/// The application must handle the interaction with the end-user.
/// </summary>
internal interface ICredentialStore {
    public IVpnCredentials? ReadCredentials(String url);

    /// <summary>
    /// Create a credential that may be used to persist credentials
    /// on successful connection.
    /// </summary>
    public IVpnCredentials? CreateCredentials(String url, String username, String password) {
        return null;
    }
}