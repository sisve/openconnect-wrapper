using System;
using System.Runtime.InteropServices;
using System.Text;

public static class CredentialManager {
    internal const Int32 NO_ERROR = 0;
    internal const Int32 ERROR_CANCELLED = 1223;
    internal const Int32 ERROR_NO_SUCH_LOGON_SESSION = 1312;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CREDUI_INFOW {
        public Int32 cbSize;

        /* HWND */
        public IntPtr hwndParent;

        /* PCWSTR */
        public String pszMessageText;

        /* PCWSTR */
        public String pszCaptionText;

        /* HBITMAP */
        public IntPtr hbmBanner;
    }

    [Flags]
    internal enum CREDUI_FLAGS : uint {
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
    internal static extern Int32 CredUIPromptForCredentialsW(
        /* PCREDUI_INFOW */ ref CREDUI_INFOW creditUR,
        /* PCWSTR */ String targetName,
        /* PCtxtHandle */ IntPtr pContext,
        Int32 iError,
        /* PWSTR */ StringBuilder userName,
        /* ULONG */ Int32 maxUserName,
        /* PWSTR */ StringBuilder password,
        /* ULONG */ Int32 maxPassword,
        /* BOOL* */ [MarshalAs(UnmanagedType.Bool)] ref Boolean pfSave,
        /* DWORD */ CREDUI_FLAGS flags
    );

    [DllImport("credui.dll", EntryPoint = "CredUIConfirmCredentialsW", CharSet = CharSet.Unicode)]
    internal static extern Int32 CredUIConfirmCredentialsW(
        /* PCWSTR */ String targetName,
        [MarshalAs(UnmanagedType.Bool)] Boolean confirm
    );
}