using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ConnectToUrl.Linux.Libsecret;

namespace ConnectToUrl.Linux;

/// <summary>
/// Credential store, based on libsecret, to store items with
/// the attributes { url: $url, type: ("username"|"password") }
/// </summary>
[SupportedOSPlatform("Linux")]
internal class LinuxCredentialStore : ICredentialStore {
    public IVpnCredentials? ReadCredentials(String url) {
        var username = GetSecret(url, "username");
        var password = GetSecret(url, "password");
        if (username != null && password != null) {
            return new PersistedCredentials(url, username, password);
        }

        return null;
    }

    public IVpnCredentials CreateCredentials(String url, String username, String password) {
        return new TransientCredentials(url, username, password);
    }

    private record CredentialsBase(String Username, String Password);

    private record PersistedCredentials(String Url, String Username, String Password) : CredentialsBase(Username, Password), IVpnCredentials {
        public void Success() {
        }

        public void Fail() {
            // Remove already persisted credentials on failure
            SetSecret(Url, "username", null);
            SetSecret(Url, "password", null);
        }
    }

    private record TransientCredentials(String Url, String Username, String Password) : CredentialsBase(Username, Password), IVpnCredentials {
        public void Success() {
            // Persist transient credentials on success
            SetSecret(Url, "username", Username);
            SetSecret(Url, "password", Password);
        }

        public void Fail() {
        }
    }

    #region libsecret wrappers

    private static IntPtr _schemaPtr;

    private static IntPtr GetSchema() {
        if (_schemaPtr == IntPtr.Zero) {
            _schemaPtr = secret_schema_new(
                "org.freedesktop.Secret.Generic",
                (Int32)SecretSchemaFlags.None,
                "url", (Int32)SecretSchemaAttributeType.String,
                "type", (Int32)SecretSchemaAttributeType.String,
                IntPtr.Zero);
        }

        return _schemaPtr;
    }

    private static String? GetSecret(String url, String type) {
        var schema = GetSchema();
        var result = secret_password_lookup_sync(schema, IntPtr.Zero, out var error,
                                                 "url", url,
                                                 "type", type,
                                                 IntPtr.Zero);

        GException.ThrowIfError(error);

        return Marshal.PtrToStringAnsi(result);
    }

    private static Boolean SetSecret(String url, String type, String? secret) {
        var schema = GetSchema();

        IntPtr error;
        Boolean result;

        if (secret != null) {
            result = secret_password_store_sync(schema, "default", $"VPN: {url}", secret, IntPtr.Zero, out error,
                                                "url", url,
                                                "type", type,
                                                IntPtr.Zero);
        } else {
            result = secret_password_clear_sync(schema, IntPtr.Zero, out error,
                                                "url", url,
                                                "type", type,
                                                IntPtr.Zero);
        }

        GException.ThrowIfError(error);

        return result;
    }

    #endregion
}