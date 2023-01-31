using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ConnectToUrl;

internal abstract unsafe class OpenConnect {
    internal const String DllName = "openconnect";
    internal const String WindowsDllName = "libopenconnect-5";

    public enum OC_FORM_OPT_TYPE {
        TEXT = 1, // OC_FORM_OPT_TEXT
        PASSWORD = 2, // OC_FORM_OPT_PASSWORD
        SELECT = 3, // OC_FORM_OPT_SELECT
        HIDDEN = 4, // OC_FORM_OPT_HIDDEN
        TOKEN = 5, // OC_FORM_OPT_TOKEN
        SSO_TOKEN = 6, // OC_FORM_OPT_SSO_TOKEN
        SSO_USER = 7, // OC_FORM_OPT_SSO_USER
    }

    [Flags]
    public enum OC_FORM_OPT_FLAGS : UInt32 {
        NONE = 0x0000,
        IGNORE = 0x0001, // OC_FORM_OPT_IGNORE
        OC_FORM_OPT_NUMERIC = 0x0002, // OC_FORM_OPT_NUMERIC
    }

    public const Int32 OC_FORM_RESULT_ERR = -1;
    public const Int32 OC_FORM_RESULT_OK = 0;
    public const Int32 OC_FORM_RESULT_CANCELLED = 1;
    public const Int32 OC_FORM_RESULT_NEWGROUP = 2;

    public const Int32 PRG_ERR = 0;
    public const Int32 PRG_INFO = 1;
    public const Int32 PRG_DEBUG = 2;
    public const Int32 PRG_TRACE = 3;

    /// <summary>
    ///   CANCEL closes network connections, logs off the session (cookie)
    ///   and shuts down the tun device.
    /// </summary>
    [NotNull]
    public static readonly Char* OC_CMD_CANCEL = Helper.StringToHGlobalAnsi("x");

    /// <summary>
    ///   PAUSE closes network connections and returns. The caller is expected
    ///   to call openconnect_mainloop() again soon
    /// </summary>
    [NotNull]
    public static readonly Char* OC_CMD_PAUSE = Helper.StringToHGlobalAnsi("p");

    /// <summary>
    ///   DETACH closes network connections and shuts down the tun device.
    ///   It is not legal to call openconnect_mainloop() again after this,
    ///   but a new instance of openconnect can be started using the same
    ///   cookie.
    /// </summary>
    [NotNull]
    public static readonly Char* OC_CMD_DETACH = Helper.StringToHGlobalAnsi("d");

    /// <summary>
    ///   STATS calls the stats_handler.
    /// </summary>
    [NotNull]
    public static readonly Char* OC_CMD_STATS = Helper.StringToHGlobalAnsi("s");

    public const Int32 RECONNECT_INTERVAL_MIN = 10;
    public const Int32 RECONNECT_INTERVAL_MAX = 100;

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_vpn_proto {
        public Char* name;
        public Char* pretty_name;
        public Char* description;
        public UInt32 flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_form_opt {
        public oc_form_opt* next;

        [OriginalType("int")]
        public OC_FORM_OPT_TYPE type;

        public Char* name;
        public Char* label;
        public Char* _value;

        [OriginalType("unsigned int")]
        public OC_FORM_OPT_FLAGS flags;

        public void* reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_choice {
        public Char* name;
        public Char* label;
        public Char* auth_type;
        public Char* override_name;
        public Char* override_label;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_form_opt_select {
        public oc_form_opt form;
        public Int32 nr_choices;
        public oc_choice** choices;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_auth_form {
        public Char* banner;
        public Char* message;
        public Char* error;
        public Char* auth_id;
        public Char* method;
        public Char* action;
        public oc_form_opt* opts;
        public oc_form_opt_select* authgroup_opt;
        public Int32 authgroup_selection;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_split_include {
        [OriginalType("const char*")]
        public Char* route;
        public oc_split_include* next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_ip_info {
        [OriginalType("const char*")]
        public Char* addr;

        /// <summary>
        ///   Just the netmask, in dotted-quad form.
        /// </summary>
        [OriginalType("const char*")]
        public Char* netmask;

        [OriginalType("const char*")]
        public Char* addr6;

        /// <summary>
        ///   This is the IPv6 address *and* netmask e.g. "2001::dead:beef/128".
        /// </summary>
        [OriginalType("const char*")]
        public Char* netmask6; /*  */

        [OriginalType("const char* dns[3]")]
        public Char* dns1;
        public Char* dns2;
        public Char* dns3;

        [OriginalType("const char* nbns[3]")]
        public Char* nbns1;
        public Char* nbns2;
        public Char* nbns3;

        [OriginalType("const char*")]
        public Char* domain;

        [OriginalType("const char*")]
        public Char* proxy_pac;
        public Int32 mtu;

        public oc_split_include* split_dns;
        public oc_split_include* split_includes;
        public oc_split_include* split_excludes;

        /* The elements above this line come from server-provided CSTP headers,
         * so they should be handled with caution.  gateway_addr is generated
         * locally from getnameinfo(). */
        public Char* gateway_addr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_vpn_option {
        public Char* option;
        public Char* value;
        public oc_vpn_option* next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct openconnect_info {
    }

    public delegate Int32 openconnect_validate_peer_cert_vfn(
        void* privdata,

        [OriginalType("const char*")]
        Char* reason
    );

    public delegate Int32 openconnect_write_new_config_vfn(
        void* privdata,

        [OriginalType("const char*")]
        Char* buf,

        Int32 buflen
    );

    public delegate Int32 openconnect_process_auth_form_vfn(
        void* privdata,

        [OriginalType("struct oc_auth_form*")]
        oc_auth_form* form
    );
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void openconnect_progress_vfn(
        void* privdata,
        Int32 level,

        [OriginalType("const char*")]
        Char* fmt,

        [OriginalType("...")]
        void* args
    );

    public delegate void openconnect_setup_tun_vfn(
        void* privdata
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_option_value")]
    public static extern Int32 openconnect_set_option_value(
        oc_form_opt* opt,

        [OriginalType("const char*")]
        String value
    );

    [DllImport(DllName, EntryPoint = "openconnect_obtain_cookie")]
    public static extern Int32 openconnect_obtain_cookie(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_reported_os")]
    public static extern Int32 openconnect_set_reported_os(
        openconnect_info* param0,

        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String os
    );

    [DllImport(DllName, EntryPoint = "openconnect_get_ip_info")]
    public static extern Int32 openconnect_get_ip_info(
        openconnect_info* vpninfo,

        [OriginalType("const struct oc_ip_info**")]
        oc_ip_info** info,

        [OriginalType("const struct oc_vpn_option**")]
        oc_vpn_option** cstp_options,

        [OriginalType("const struct oc_vpn_option**")]
        oc_vpn_option** dtls_options
    );

    [DllImport(DllName, EntryPoint = "openconnect_disable_ipv6")]
    public static extern Int32 openconnect_disable_ipv6(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_disable_dtls")]
    public static extern Int32 openconnect_disable_dtls(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_reset_ssl")]
    public static extern void openconnect_reset_ssl(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_parse_url")]
    public static extern Int32 openconnect_parse_url(
        openconnect_info* vpninfo,

        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String url
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_cert_expiry_warning")]
    public static extern void openconnect_set_cert_expiry_warning(
        openconnect_info* vpninfo,
        Int32 seconds
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_pfs")]
    public static extern void openconnect_set_pfs(
        openconnect_info* vpninfo,
        UInt32 val
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_allow_insecure_crypto")]
    public static extern Int32 openconnect_set_allow_insecure_crypto(
        openconnect_info* vpninfo,
        UInt32 val
    );

    [DllImport(DllName, EntryPoint = "openconnect_setup_cmd_pipe", SetLastError = true)]
    public static extern Int32 openconnect_setup_cmd_pipe(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_make_cstp_connection")]
    public static extern Int32 openconnect_make_cstp_connection(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_setup_tun_device")]
    public static extern Int32 openconnect_setup_tun_device(
        openconnect_info* vpninfo,

        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String? vpnc_script,

        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String? ifname
    );

    [DllImport(DllName, EntryPoint = "openconnect_mainloop")]
    public static extern Int32 openconnect_mainloop(
        openconnect_info* vpninfo,
        Int32 reconnect_timeout,
        Int32 reconnect_interval
    );

    [DllImport(DllName, EntryPoint = "openconnect_vpninfo_new")]
    [return: NotNull]
    public static extern openconnect_info* openconnect_vpninfo_new(
        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String useragent,

        openconnect_validate_peer_cert_vfn? param1,
        openconnect_write_new_config_vfn? param2,
        openconnect_process_auth_form_vfn? param3,
        openconnect_progress_vfn? param4,

        void* privdata
    );

    [DllImport(DllName, EntryPoint = "openconnect_vpninfo_free")]
    public static extern void openconnect_vpninfo_free(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_get_supported_protocols")]
    public static extern Int32 openconnect_get_supported_protocols(
        oc_vpn_proto** protos
    );

    [DllImport(DllName, EntryPoint = "openconnect_free_supported_protocols")]
    public static extern void openconnect_free_supported_protocols(
        oc_vpn_proto* protos
    );

    [DllImport(DllName, EntryPoint = "openconnect_get_protocol")]
    public static extern Char* openconnect_get_protocol(
        openconnect_info* vpninfo
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_protocol")]
    public static extern Int32 openconnect_set_protocol(
        openconnect_info* vpninfo,

        [OriginalType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String protocol
    );

    [DllImport(DllName, EntryPoint = "openconnect_set_setup_tun_handler")]
    public static extern void openconnect_set_setup_tun_handler(
        openconnect_info* vpninfo,
        openconnect_setup_tun_vfn setup_tun
    );
}