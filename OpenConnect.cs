using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ConnectToUrl;

internal abstract unsafe class OpenConnect {
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

    [NotNull]
    public static readonly Char* OC_CMD_CANCEL = Helper.StringToHGlobalAnsi("x");

    [NotNull]
    public static readonly Char* OC_CMD_PAUSE = Helper.StringToHGlobalAnsi("p");

    [NotNull]
    public static readonly Char* OC_CMD_DETACH = Helper.StringToHGlobalAnsi("d");

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
        /* int */ public OC_FORM_OPT_TYPE type;
        public Char* name;
        public Char* label;
        public Char* _value;
        /* unsigned int */ public OC_FORM_OPT_FLAGS flags;
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
        /*const*/
        public Char* route;
        public oc_split_include* next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct oc_ip_info {
        /*const*/
        public Char* addr;

        /*const*/
        public Char* netmask; /* Just the netmask, in dotted-quad form. */

        /*const*/
        public Char* addr6;

        /*const*/
        public Char* netmask6; /* This is the IPv6 address *and* netmask
			       * e.g. "2001::dead:beef/128". */

        /*const char *dns[3];*/
        public Char* dns1;
        public Char* dns2;
        public Char* dns3;

        /*const char *nbns[3]*/
        public Char* nbns1;
        public Char* nbns2;
        public Char* nbns3;

        /*const*/
        public Char* domain;

        /*const*/
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
        Char* reason
    );

    public delegate Int32 openconnect_write_new_config_vfn(
        void* privdata,
        Char* buf,
        Int32 buflen
    );

    public delegate Int32 openconnect_process_auth_form_vfn(
        void* privdata,
        oc_auth_form* form
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void openconnect_progress_vfn(
        void* privdata,
        Int32 level,
        /* const char* */ /*[In, MarshalAs(UnmanagedType.LPStr)] String*/ Char* fmt,
        /* ... */ void* args
    );

    public delegate void openconnect_setup_tun_vfn(
        void* privdata
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_option_value")]
    public static extern Int32 openconnect_set_option_value(
        oc_form_opt* opt,
        /* const char* */ [MarshalAs(UnmanagedType.LPStr)] String value
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_obtain_cookie")]
    public static extern Int32 openconnect_obtain_cookie(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_reported_os")]
    public static extern Int32 openconnect_set_reported_os(
        openconnect_info* param0,
        /* const char* */ [In, MarshalAs(UnmanagedType.LPStr)] String os
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_get_ip_info")]
    public static extern Int32 openconnect_get_ip_info(
        openconnect_info* vpninfo,
        /* const struct */
        oc_ip_info** info,
        /* const struct */
        oc_vpn_option** cstp_options,
        /* const struct */
        oc_vpn_option** dtls_options
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_disable_ipv6")]
    public static extern Int32 openconnect_disable_ipv6(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_disable_dtls")]
    public static extern Int32 openconnect_disable_dtls(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_reset_ssl")]
    public static extern void openconnect_reset_ssl(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_parse_url")]
    public static extern Int32 openconnect_parse_url(
        openconnect_info* vpninfo,
        /* const char* */ [In, MarshalAs(UnmanagedType.LPStr)] String url
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_cert_expiry_warning")]
    public static extern void openconnect_set_cert_expiry_warning(
        openconnect_info* vpninfo,
        Int32 seconds
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_pfs")]
    public static extern void openconnect_set_pfs(
        openconnect_info* vpninfo,
        UInt32 val
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_allow_insecure_crypto")]
    public static extern Int32 openconnect_set_allow_insecure_crypto(
        openconnect_info* vpninfo,
        UInt32 val
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_setup_cmd_pipe", SetLastError = true)]
    public static extern Int32 openconnect_setup_cmd_pipe(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_make_cstp_connection")]
    public static extern Int32 openconnect_make_cstp_connection(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_setup_tun_device")]
    public static extern Int32 openconnect_setup_tun_device(
        openconnect_info* vpninfo,
        /* const char* */ [MarshalAs(UnmanagedType.LPStr)] String? vpnc_script,
        /* const char* */ [MarshalAs(UnmanagedType.LPStr)] String? ifname
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_mainloop")]
    public static extern Int32 openconnect_mainloop(
        openconnect_info* vpninfo,
        Int32 reconnect_timeout,
        Int32 reconnect_interval
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_vpninfo_new")]
    [return: NotNull]
    public static extern openconnect_info* openconnect_vpninfo_new(
        /* const char* */ [In, MarshalAs(UnmanagedType.LPStr)] String useragent,
        openconnect_validate_peer_cert_vfn? param1,
        openconnect_write_new_config_vfn? param2,
        openconnect_process_auth_form_vfn? param3,
        openconnect_progress_vfn? param4,
        void* privdata
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_vpninfo_free")]
    public static extern void openconnect_vpninfo_free(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_get_supported_protocols")]
    public static extern Int32 openconnect_get_supported_protocols(
        oc_vpn_proto** protos
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_free_supported_protocols")]
    public static extern void openconnect_free_supported_protocols(
        oc_vpn_proto* protos
    );


    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_get_protocol")]
    public static extern Char* openconnect_get_protocol(
        openconnect_info* vpninfo
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_protocol")]
    public static extern Int32 openconnect_set_protocol(
        openconnect_info* vpninfo,
        /* const char* */ [In, MarshalAs(UnmanagedType.LPStr)] String protocol
    );

    [DllImport("libopenconnect-5.dll", EntryPoint = "openconnect_set_setup_tun_handler")]
    public static extern void openconnect_set_setup_tun_handler(
        openconnect_info* vpninfo,
        openconnect_setup_tun_vfn setup_tun
    );
}