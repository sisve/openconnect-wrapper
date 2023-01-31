using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ConnectToUrl;

internal abstract unsafe class OpenConnect {
    internal const String DllName = "openconnect";
    internal const String WindowsDllName = "libopenconnect-5";

    [SourceReference("openconnect.h", 218, 224)]
    public enum OC_FORM_OPT_TYPE {
        TEXT = 1, // OC_FORM_OPT_TEXT
        PASSWORD = 2, // OC_FORM_OPT_PASSWORD
        SELECT = 3, // OC_FORM_OPT_SELECT
        HIDDEN = 4, // OC_FORM_OPT_HIDDEN
        TOKEN = 5, // OC_FORM_OPT_TOKEN
        SSO_TOKEN = 6, // OC_FORM_OPT_SSO_TOKEN
        SSO_USER = 7, // OC_FORM_OPT_SSO_USER
    }
    
    [SourceReference("openconnect.h", 226, 229)]
    public enum OC_FORM_RESULT {
        ERR = -1, // OC_FORM_RESULT_ERR 
        OK = 0, // OC_FORM_RESULT_OK
        CANCELLED = 1, // OC_FORM_RESULT_CANCELLED
        NEWGROUP = 2, // OC_FORM_RESULT_NEWGROUP
    }

    [SourceReference("openconnect.h", 237, 238)]
    [Flags]
    public enum OC_FORM_OPT_FLAGS : UInt32 {
        NONE = 0x0000,
        IGNORE = 0x0001, // OC_FORM_OPT_IGNORE
        OC_FORM_OPT_NUMERIC = 0x0002, // OC_FORM_OPT_NUMERIC
    }
    
    /// <remarks>
    ///   char * fields are static (owned by XML parser) and don't need to be
    ///   freed by the form handling code — except for value, which for TEXT
    ///   and PASSWORD options is allocated by openconnect_set_option_value()
    ///   when process_form() interacts with the user and must be freed.
    /// </remarks>
    [SourceReference("openconnect.h", 244, 252)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_form_opt {
        public oc_form_opt* next;

        [SourceType("int")]
        public OC_FORM_OPT_TYPE type;

        public Char* name;
        public Char* label;
        
        /// <summary>
        ///   Use openconnect_set_option_value() to set this
        /// </summary>
        public Char* _value;

        [SourceType("unsigned int")]
        public OC_FORM_OPT_FLAGS flags;

        public void* reserved;
    }
    
    /// <summary>
    ///   To set the value to a form use the following function
    /// </summary>
    [SourceReference("openconnect.h", 255)]
    [DllImport(DllName, EntryPoint = "openconnect_set_option_value")]
    public static extern Int32 openconnect_set_option_value(
        oc_form_opt* opt,

        [SourceType("const char*")]
        String value
    );

    /// <remarks>
    ///   All fields are static, owned by the XML parser
    /// </remarks>
    [SourceReference("openconnect.h", 258, 270)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_choice {
        public Char* name;
        public Char* label;
        public Char* auth_type;
        public Char* override_name;
        public Char* override_label;
    }

    [SourceReference("openconnect.h", 272, 276)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_form_opt_select {
        public oc_form_opt form;
        public Int32 nr_choices;
        public oc_choice** choices;
    }

    /// <remarks>
    ///  All char * fields are static, owned by the XML parser
    /// </remarks>
    [SourceReference("openconnect.h", 279, 289)]
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

    [SourceReference("openconnect.h", 291, 294)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_split_include {
        [SourceType("const char*")]
        public Char* route;
        public oc_split_include* next;
    }

    [SourceReference("openconnect.h", 296, 316)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_ip_info {
        [SourceType("const char*")]
        public Char* addr;

        /// <summary>
        ///   Just the netmask, in dotted-quad form.
        /// </summary>
        [SourceType("const char*")]
        public Char* netmask;

        [SourceType("const char*")]
        public Char* addr6;

        /// <summary>
        ///   This is the IPv6 address *and* netmask e.g. "2001::dead:beef/128".
        /// </summary>
        [SourceType("const char*")]
        public Char* netmask6; /*  */

        [SourceType("const char* dns[3]")]
        public Char* dns1;
        public Char* dns2;
        public Char* dns3;

        [SourceType("const char* nbns[3]")]
        public Char* nbns1;
        public Char* nbns2;
        public Char* nbns3;

        [SourceType("const char*")]
        public Char* domain;

        [SourceType("const char*")]
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

    [SourceReference("openconnect.h", 318, 322)]
    [StructLayout(LayoutKind.Sequential)]
    public struct oc_vpn_option {
        public Char* option;
        public Char* value;
        public oc_vpn_option* next;
    }

    [SourceReference("openconnect.h", 350)]
    public const Int32 PRG_ERR = 0;

    [SourceReference("openconnect.h", 351)]
    public const Int32 PRG_INFO = 1;

    [SourceReference("openconnect.h", 352)]
    public const Int32 PRG_DEBUG = 2;
    
    [SourceReference("openconnect.h", 353)]
    public const Int32 PRG_TRACE = 3;

    /// <summary>
    ///   CANCEL closes network connections, logs off the session (cookie)
    ///   and shuts down the tun device.
    /// </summary>
    [SourceReference("openconnect.h", 367)]
    [NotNull]
    public static readonly Char* OC_CMD_CANCEL = Helper.StringToHGlobalAnsi("x");

    /// <summary>
    ///   PAUSE closes network connections and returns. The caller is expected
    ///   to call openconnect_mainloop() again soon
    /// </summary>
    [SourceReference("openconnect.h", 368)]
    [NotNull]
    public static readonly Char* OC_CMD_PAUSE = Helper.StringToHGlobalAnsi("p");

    /// <summary>
    ///   DETACH closes network connections and shuts down the tun device.
    ///   It is not legal to call openconnect_mainloop() again after this,
    ///   but a new instance of openconnect can be started using the same
    ///   cookie.
    /// </summary>
    [SourceReference("openconnect.h", 369)]
    [NotNull]
    public static readonly Char* OC_CMD_DETACH = Helper.StringToHGlobalAnsi("d");

    /// <summary>
    ///   STATS calls the stats_handler.
    /// </summary>
    [SourceReference("openconnect.h", 370)]
    [NotNull]
    public static readonly Char* OC_CMD_STATS = Helper.StringToHGlobalAnsi("s");

    [SourceReference("openconnect.h", 372)]
    public const Int32 RECONNECT_INTERVAL_MIN = 10;
  
    [SourceReference("openconnect.h", 373)]
    public const Int32 RECONNECT_INTERVAL_MAX = 100;

    [SourceReference("openconnect.h", 375)]
    [StructLayout(LayoutKind.Sequential)]
    public struct openconnect_info {
    }

    [SourceReference("openconnect.h", 470)]
    [DllImport(DllName, EntryPoint = "openconnect_obtain_cookie")]
    public static extern Int32 openconnect_obtain_cookie(
        openconnect_info* vpninfo
    );

    /// <summary>
    ///   Valid choices are: "linux", "linux-64", "win", "mac-intel",
    ///   "android", and "apple-ios". This also selects the corresponding CSD
    ///   trojan binary.
    /// </summary>
    [SourceReference("openconnect.h", 628)]
    [DllImport(DllName, EntryPoint = "openconnect_set_reported_os")]
    public static extern Int32 openconnect_set_reported_os(
        openconnect_info* param0,

        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String os
    );

    /// <summary>
    ///   The returned structures are owned by the library and may be freed/replaced
    ///   due to rekey or reconnect. Assume that once the mainloop starts, the
    ///   pointers are no longer valid. For similar reasons, it is unsafe to call
    ///   this function from another thread.
    /// </summary>
    [SourceReference("openconnect.h", 658, 661)]
    [DllImport(DllName, EntryPoint = "openconnect_get_ip_info")]
    public static extern Int32 openconnect_get_ip_info(
        openconnect_info* vpninfo,

        [SourceType("const struct oc_ip_info**")]
        oc_ip_info** info,

        [SourceType("const struct oc_vpn_option**")]
        oc_vpn_option** cstp_options,

        [SourceType("const struct oc_vpn_option**")]
        oc_vpn_option** dtls_options
    );
    
    [SourceReference("openconnect.h", 668)]
    [DllImport(DllName, EntryPoint = "openconnect_disable_ipv6")]
    public static extern Int32 openconnect_disable_ipv6(
        openconnect_info* vpninfo
    );

    [SourceReference("openconnect.h", 671)]
    [DllImport(DllName, EntryPoint = "openconnect_parse_url")]
    public static extern Int32 openconnect_parse_url(
        openconnect_info* vpninfo,

        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String url
    );

    [SourceReference("openconnect.h", 674)]
    [DllImport(DllName, EntryPoint = "openconnect_set_pfs")]
    public static extern void openconnect_set_pfs(
        openconnect_info* vpninfo,
        UInt32 val
    );
    
    /// <summary>
    ///   Create a nonblocking pipe used to send cancellations and other commands
    ///   to the library. This returns a file descriptor to the write side of
    ///   the pipe. Both sides will be closed by openconnect_vpninfo_free().
    ///   This replaces openconnect_set_cancel_fd().
    /// </summary>
    [SourceReference("openconnect.h", 692, 697)]
    [return: SourceType("SOCKET on _WIN32, int")]
    [DllImport(DllName, EntryPoint = "openconnect_setup_cmd_pipe", SetLastError = true)]
    public static extern Int32 openconnect_setup_cmd_pipe(
        openconnect_info* vpninfo
    );

    /// <summary>
    ///   Open CSTP connection; on success, IP information will be available.
    /// </summary>
    [SourceReference("openconnect.h", 702)]
    [DllImport(DllName, EntryPoint = "openconnect_make_cstp_connection")]
    public static extern Int32 openconnect_make_cstp_connection(
        openconnect_info* vpninfo
    );
    
    /// <summary>
    ///   Create a tun device through the OS kernel (typical use case). Both
    ///   strings are optional and can be NULL if desired.
    /// </summary>
    [SourceReference("openconnect.h", 706, 707)]
    [DllImport(DllName, EntryPoint = "openconnect_setup_tun_device")]
    public static extern Int32 openconnect_setup_tun_device(
        openconnect_info* vpninfo,

        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String? vpnc_script,

        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String? ifname
    );

    /// <summary>
    ///   Start the main loop; exits if OC_CMD_CANCEL is received on cmd_fd or
    ///   the remote site aborts.
    /// </summary>
    [SourceReference("openconnect.h", 725, 727)]
    [DllImport(DllName, EntryPoint = "openconnect_mainloop")]
    public static extern Int32 openconnect_mainloop(
        openconnect_info* vpninfo,
        Int32 reconnect_timeout,
        Int32 reconnect_interval
    );
    
    /// <summary>
    ///   When the server's certificate fails validation via the normal means,
    ///   this function is called with the offending certificate along with
    ///   a textual reason for the failure (which may not be translated, if
    ///   it comes directly from OpenSSL, but will be if it is rejected for
    ///   "certificate does not match hostname", because that check is done
    ///   in OpenConnect and *is* translated). The function shall return zero
    ///   if the certificate is (or has in the past been) explicitly accepted
    ///   by the user, and non-zero to abort the connection.
    /// </summary>
    [SourceReference("openconnect.h", 741, 742)]
    public delegate Int32 openconnect_validate_peer_cert_vfn(
        void* privdata,

        [SourceType("const char*")]
        Char* reason
    );

    /// <summary>
    ///   On a successful connection, the server may provide us with a new XML
    ///   configuration file. This contains the list of servers that can be
    ///   chosen by the user to connect to, amongst other stuff that we mostly
    ///   ignore. By "new", we mean that the SHA1 indicated by the server does
    ///   not match the SHA1 set with the openconnect_set_xmlsha1() above. If
    ///   they don't match, or openconnect_set_xmlsha1() has not been called,
    ///   then the new XML is downloaded and this function is invoked.
    /// </summary>
    [SourceReference("openconnect.h", 750, 751)]    
    public delegate Int32 openconnect_write_new_config_vfn(
        void* privdata,

        [SourceType("const char*")]
        Char* buf,

        Int32 buflen
    );

    /// <summary>
    ///   Handle an authentication form, requesting input from the user.
    ///   Return value:
    ///   *  &lt; 0, on error
    ///   *  = 0, when form was parsed and POST required
    ///   *  = 1, when response was cancelled by user
    /// </summary>
    [SourceReference("openconnect.h", 758, 759)]
    [return: SourceType("int")]
    public delegate OC_FORM_RESULT openconnect_process_auth_form_vfn(
        void* privdata,

        [SourceType("struct oc_auth_form*")]
        oc_auth_form* form
    );
    
    /// <summary>
    ///   Logging output which the user *may* want to see.
    /// </summary>
    /// <remarks>
    ///   The va_list (...) makes this very hard to use in managed code.
    ///   We're using a small native library that handles the variadic part,
    ///   and forward the generated logging string to managed code.
    ///   See <see cref="Logger" />.
    /// </remarks>
    [SourceReference("openconnect.h", 761, 763)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void openconnect_progress_vfn(
        void* privdata,
        Int32 level,

        [SourceType("const char*")]
        Char* fmt,

        [SourceType("...")]
        void* args
    );
    
    [SourceReference("openconnect.h", 764, 769)]
    [DllImport(DllName, EntryPoint = "openconnect_vpninfo_new")]
    [return: NotNull]
    public static extern openconnect_info* openconnect_vpninfo_new(
        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String useragent,

        openconnect_validate_peer_cert_vfn? param1,
        openconnect_write_new_config_vfn? param2,
        openconnect_process_auth_form_vfn? param3,
        openconnect_progress_vfn? param4,

        void* privdata
    );
    
    [SourceReference("openconnect.h", 770)]
    [DllImport(DllName, EntryPoint = "openconnect_vpninfo_free")]
    public static extern void openconnect_vpninfo_free(
        openconnect_info* vpninfo
    );
    
    /// <summary>
    ///   Callback for configuring the interface after tunnel is fully up.
    /// </summary>
    [SourceReference("openconnect.h", 829)]
    public delegate void openconnect_setup_tun_vfn(
        void* privdata
    );

    [SourceReference("openconnect.h", 830)]
    [DllImport(DllName, EntryPoint = "openconnect_set_setup_tun_handler")]
    public static extern void openconnect_set_setup_tun_handler(
        openconnect_info* vpninfo,
        openconnect_setup_tun_vfn setup_tun
    );

    [SourceReference("openconnect.h", 821)]
    [DllImport(DllName, EntryPoint = "openconnect_set_protocol")]
    public static extern Int32 openconnect_set_protocol(
        openconnect_info* vpninfo,

        [SourceType("const char*")]
        [MarshalAs(UnmanagedType.LPStr)]
        String protocol
    );
}