using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ConnectToUrl.Windows;

[SupportedOSPlatform("Windows")]
internal class WindowsCredentialManager : ICredentialManager {
    private const Int32 NO_ERROR = 0;
    private const Int32 ERROR_CANCELLED = 1223;
    private const Int32 ERROR_NO_SUCH_LOGON_SESSION = 1312;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDUI_INFOW {
        public Int32 cbSize;

        [OriginalType("HWND")]
        public IntPtr hwndParent;

        [OriginalType("PCWSTR")]
        public String pszMessageText;

        [OriginalType("PCWSTR")]
        public String pszCaptionText;

        [OriginalType("HBITMAP")]
        public IntPtr hbmBanner;
    }

    [Flags]
    private enum CREDUI_FLAGS : uint {
        CREDUI_FLAGS_INCORRECT_PASSWORD = 0x00001,
        CREDUI_FLAGS_DO_NOT_PERSIST = 0x00002,
        CREDUI_FLAGS_REQUEST_ADMINISTRATOR = 0x00004,
        CREDUI_FLAGS_EXCLUDE_CERTIFICATES = 0x00008,
        CREDUI_FLAGS_REQUIRE_CERTIFICATE = 0x00010,
        CREDUI_FLAGS_SHOW_SAVE_CHECK_BOX = 0x00040,
        CREDUI_FLAGS_ALWAYS_SHOW_UI = 0x00080,
        CREDUI_FLAGS_REQUIRE_SMARTCARD = 0x00100,
        CREDUI_FLAGS_PASSWORD_ONLY_OK = 0x00200,
        CREDUI_FLAGS_VALIDATE_USERNAME = 0x00400,
        CREDUI_FLAGS_COMPLETE_USERNAME = 0x00800,
        CREDUI_FLAGS_PERSIST = 0x01000,
        CREDUI_FLAGS_SERVER_CREDENTIAL = 0x04000,
        CREDUI_FLAGS_EXPECT_CONFIRMATION = 0x20000,
        CREDUI_FLAGS_GENERIC_CREDENTIALS = 0x40000,
        CREDUI_FLAGS_USERNAME_TARGET_CREDENTIALS = 0x80000,
        CREDUI_FLAGS_KEEP_USERNAME = 0x100000,
    }

    [DllImport("credui.dll", EntryPoint = "CredUIPromptForCredentialsW", CharSet = CharSet.Unicode)]
    private static extern Int32 CredUIPromptForCredentialsW(
        [OriginalType("PCREDUI_INFOW")]
        ref CREDUI_INFOW creditUR,

        [OriginalType("PCWSTR")]
        String targetName,

        [OriginalType("PCtxtHandle")]
        IntPtr pContext,

        Int32 iError,

        [OriginalType("PWSTR")]
        StringBuilder userName,

        [OriginalType("ULONG")]
        Int32 maxUserName,

        [OriginalType("PWSTR")]
        StringBuilder password,

        [OriginalType("ULONG")]
        Int32 maxPassword,

        [MarshalAs(UnmanagedType.Bool)]
        ref Boolean pfSave,

        [OriginalType("DWORD")]
        CREDUI_FLAGS flags
    );

    [DllImport("credui.dll", EntryPoint = "CredUIConfirmCredentialsW", CharSet = CharSet.Unicode)]
    private static extern Int32 CredUIConfirmCredentialsW(
        [OriginalType("PCWSTR")]
        String targetName,

        [MarshalAs(UnmanagedType.Bool)]
        Boolean confirm
    );

    public IVpnCredentials? AskForCredentials(String url, String messageText) {
        return AskForCredentials(url, messageText, 0, false);
    }

    public IVpnCredentials? ForceAskForCredentials(String url, String messageText) {
        const Int32 ERROR_NETWORK_ACCESS_DENIED = 65;
        return AskForCredentials(url, messageText, ERROR_NETWORK_ACCESS_DENIED, true);
    }
    
    [SupportedOSPlatform("Windows")]
    private static IVpnCredentials? AskForCredentials(String url, String messageText, Int32 previousError, Boolean forceShowUI) {
        var credReq = new CREDUI_INFOW {
            pszCaptionText = "VPN credentials",
            pszMessageText = messageText,
        };

        credReq.cbSize = Marshal.SizeOf(credReq);

        var performSave = false;
        var shouldConfirm = true;

        var targetName = "VPN: " + url;
        var maxUsernameLength = 100;
        var maxPasswordLength = 100;
        var usernameBuf = new StringBuilder(maxUsernameLength);
        var passwordBuf = new StringBuilder(maxPasswordLength);

        var flags =
            CREDUI_FLAGS.CREDUI_FLAGS_EXCLUDE_CERTIFICATES |
            CREDUI_FLAGS.CREDUI_FLAGS_SHOW_SAVE_CHECK_BOX |
            CREDUI_FLAGS.CREDUI_FLAGS_GENERIC_CREDENTIALS |
            CREDUI_FLAGS.CREDUI_FLAGS_EXPECT_CONFIRMATION;

        if (previousError != 0) {
            flags |= CREDUI_FLAGS.CREDUI_FLAGS_INCORRECT_PASSWORD;
        }

        if (forceShowUI) {
            flags |= CREDUI_FLAGS.CREDUI_FLAGS_ALWAYS_SHOW_UI;
        }

        var promptResult = CredUIPromptForCredentialsW(
            ref credReq,
            targetName,
            IntPtr.Zero,
            previousError,
            usernameBuf, maxUsernameLength,
            passwordBuf, maxPasswordLength,
            ref performSave,
            flags
        );

        if (promptResult == ERROR_NO_SUCH_LOGON_SESSION) {
            // Retry without persisting.
            shouldConfirm = false;
            flags =
                CREDUI_FLAGS.CREDUI_FLAGS_DO_NOT_PERSIST |
                CREDUI_FLAGS.CREDUI_FLAGS_EXCLUDE_CERTIFICATES |
                CREDUI_FLAGS.CREDUI_FLAGS_GENERIC_CREDENTIALS;

            if (previousError != 0) {
                flags |= CREDUI_FLAGS.CREDUI_FLAGS_INCORRECT_PASSWORD;
            }

            if (forceShowUI) {
                flags |= CREDUI_FLAGS.CREDUI_FLAGS_ALWAYS_SHOW_UI;
            }

            promptResult = CredUIPromptForCredentialsW(
                ref credReq,
                $"VPN: {url}",
                IntPtr.Zero,
                previousError,
                usernameBuf, maxUsernameLength,
                passwordBuf, maxPasswordLength,
                ref performSave,
                flags
            );
        }

        if (promptResult == ERROR_CANCELLED) {
            return null;
        }

        if (promptResult != NO_ERROR) {
            Console.Error.WriteLine($"CredUIPromptForCredentials returned error {promptResult} with flags={flags}");
            return null;
        }

        return new Credentials(targetName, usernameBuf.ToString(), passwordBuf.ToString(), shouldConfirm);
    }

    private class Credentials : IVpnCredentials {
        private readonly String _targetName;
        private readonly Boolean _shouldConfirm;

        public Credentials(String targetName, String username, String password, Boolean shouldConfirm) {
            _targetName = targetName;
            _shouldConfirm = shouldConfirm;
            Username = username;
            Password = password;
        }

        public String Username { get; }
        public String Password { get; }

        public void Fail() {
            if (_shouldConfirm) {
                // We do not care about the result.
                _ = CredUIConfirmCredentialsW(_targetName, false);
            }
        }

        public void Success() {
            if (_shouldConfirm) {
                // We do not care about the result.
                _ = CredUIConfirmCredentialsW(_targetName, true);
            }
        }
    }
}