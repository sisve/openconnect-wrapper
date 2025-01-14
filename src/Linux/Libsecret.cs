using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ConnectToUrl.Linux;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[SupportedOSPlatform("Linux")]
internal static class Libsecret {
    private const String DllName = "libsecret-1.so.0";

    // https://docs.gtk.org/glib/struct.Error.html
    internal struct GError {
        [SourceType("GQuark")] // https://docs.gtk.org/glib/alias.Quark.html
        public UInt32 domain;

        public Int32 code;

        public IntPtr message;
    }

    internal class GException(String message, UInt32 domain, Int32 code) : Exception(message) {
        public UInt32 Domain { get; } = domain;
        public Int32 Code { get; } = code;

        public static void ThrowIfError(IntPtr maybeError) {
            if (maybeError != IntPtr.Zero) {
                // TODO: Should we free the ptr?
                var error = Marshal.PtrToStructure<GError>(maybeError);
                var message = Marshal.PtrToStringAnsi(error.message) ?? String.Empty;
                throw new GException(message, error.domain, error.code);
            }
        }
    }

    // https://gnome.pages.gitlab.gnome.org/libsecret/enum.SchemaAttributeType.html
    internal enum SecretSchemaAttributeType {
        String = 0,
        Int32 = 1,
        Boolean = 2,
    }

    // https://gnome.pages.gitlab.gnome.org/libsecret/flags.SchemaFlags.html
    internal enum SecretSchemaFlags {
        None = 0,
        DontMatchName = 1,
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern Boolean secret_password_clear_sync(
        [SourceType("const SecretSchema*")] IntPtr schema,
        [SourceType("GCancellable*")] IntPtr cancellable,
        [SourceType("GError**")] out IntPtr error,
        String attribute1,
        String value1,
        String attribute2,
        String value2,
        IntPtr end // null terminated
    );

    // https://gnome.pages.gitlab.gnome.org/libsecret/func.password_lookup_sync.html
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: SourceType("gchar*")]
    internal static extern IntPtr secret_password_lookup_sync(
        [SourceType("const SecretSchema*")] IntPtr schema,
        [SourceType("GCancellable*")] IntPtr cancellable,
        [SourceType("GError**")] out IntPtr error,
        String attribute1,
        String value1,
        String attribute2,
        String value2,
        IntPtr end // null terminated
    );

    // https://gnome.pages.gitlab.gnome.org/libsecret/func.password_store_sync.html
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern Boolean secret_password_store_sync(
        [SourceType("const SecretSchema*")] IntPtr schema,
        [SourceType("const gchar*")] String collection,
        [SourceType("const gchar*")] String label,
        [SourceType("const gchar*")] String password,
        [SourceType("GCancellable*")] IntPtr cancellable,
        [SourceType("GError**")] out IntPtr error,
        String attribute1,
        String value1,
        String attribute2,
        String value2,
        IntPtr end // null terminated
    );


    // https://gnome.pages.gitlab.gnome.org/libsecret/ctor.Schema.new.html
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: SourceType("SecretSchema*")]
    internal static extern IntPtr secret_schema_new(
        [SourceType("const gchar*")] String name,
        [SourceType("SecretSchemaFlags")] Int32 flags,
        String attribute1,
        Int32 type1,
        String attribute2,
        Int32 type2,
        IntPtr end // null terminated
    );
}