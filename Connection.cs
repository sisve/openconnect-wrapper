using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static CredentialManager;
using static OpenConnect;
using static Winsock2;

namespace ConnectToUrl;

internal unsafe class Connection {
    private const Int32 SUCCESS = 0;
    private const Int32 FAILURE = 1;

    private const Int32 INVALID_SOCKET = -1;

    // https://learn.microsoft.com/en-us/cpp/c-runtime-library/errno-constants
    private const Int32 EINTR = 4;
    private const Int32 ECONNABORTED = 106;
    private const Int32 EPIPE = 32;
    private const Int32 EPERM = 1;

    private readonly ManualResetEventSlim _shouldExit = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _hasDisconnected = new ManualResetEventSlim();
    private readonly Thread _loopThread;

    // We're holding on to the delegate here, to make sure they are never garbage collected.
    private readonly openconnect_validate_peer_cert_vfn? ValidatePeerDelegate;
    private readonly openconnect_write_new_config_vfn? WriteNewConfigDelegate;
    private readonly openconnect_process_auth_form_vfn? ProcessAuthFormDelegate;
    private readonly openconnect_progress_vfn? ProgressDelegate;

    private openconnect_info* _vpninfo;
    private Int32 _cmd_fd;

    private Boolean _isFirstAuthAttempt = true;
    private Credentials? _currentCredentials;

    internal Connection() {
        _loopThread = new Thread(MainLoop);

        ValidatePeerDelegate = null;
        WriteNewConfigDelegate = null;
        ProcessAuthFormDelegate = ProcessAuthForm;
        ProgressDelegate = ReportProgress;
    }

    public String? Url { get; set; }
    public Int32 MinLoggingLevel { get; set; }
    public String? ScriptPath { get; set; }
    public String? SecondaryPassword { get; set; }

    internal Int32 Connect() {
        if (Url == null) {
            Console.Error.WriteLine("No Url specified.");
            return FAILURE;
        }

        // init winsock
        var wsaResult = WSAStartup(Helper.MakeWord(1, 1), out _);
        if (wsaResult != 0) {
            Console.Error.WriteLine($"WSAStartup failed with {wsaResult}");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-wsastartup");
            return FAILURE;
        }

        _vpninfo = openconnect_vpninfo_new(
            "Open AnyConnect VPN Agent",
            ValidatePeerDelegate,
            WriteNewConfigDelegate,
            ProcessAuthFormDelegate,
            ProgressDelegate,
            null
        );

        _cmd_fd = openconnect_setup_cmd_pipe(_vpninfo);
        if (_cmd_fd == INVALID_SOCKET) {
            var lastError = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"openconnect_setup_cmd_pipe returned error {lastError} when setting up cmd_fd.");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        var mode = 0u; // blocking
        var FIONBIO = -2147195266;
        var ioctlResult = ioctlsocket(new IntPtr(_cmd_fd), FIONBIO, &mode);
        if (ioctlResult != 0) {
            Console.Error.WriteLine($"ioctlsocket returned error {ioctlResult}");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-ioctlsocket");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        var setProtoResult = openconnect_set_protocol(_vpninfo, "anyconnect");
        if (setProtoResult != 0) {
            Console.Error.WriteLine($"openconnect_set_protocol returned error {setProtoResult}");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        openconnect_set_setup_tun_handler(_vpninfo, SetupTunHandler);
        openconnect_disable_ipv6(_vpninfo);
        openconnect_set_pfs(_vpninfo, 1);

        var parseUrlResult = openconnect_parse_url(_vpninfo, Url);
        if (parseUrlResult != 0) {
            Console.Error.WriteLine($"openconnect_parse_url returned error {parseUrlResult}");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        var setReportedOsResult = openconnect_set_reported_os(_vpninfo, "win");
        if (setReportedOsResult != 0) {
            Console.Error.WriteLine($"openconnect_set_reported_os returned error {setReportedOsResult}");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        var optainCookieResult = openconnect_obtain_cookie(_vpninfo);
        if (optainCookieResult != 0) {
            Console.Error.WriteLine($"openconnect_obtain_cookie returned error {optainCookieResult}");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        // Mark current credentials as working.
        _currentCredentials?.Success();

        var makeCstpConnectionResult = openconnect_make_cstp_connection(_vpninfo);
        if (makeCstpConnectionResult != 0) {
            Console.Error.WriteLine($"openconnect_make_cstp_connection returned error {makeCstpConnectionResult}");

            openconnect_vpninfo_free(_vpninfo);
            return FAILURE;
        }

        if (ScriptPath != null) {
            var setupTunDeviceResult = openconnect_setup_tun_device(_vpninfo, ScriptPath, null);
            if (setupTunDeviceResult != 0) {
                Console.Error.WriteLine($"openconnect_setup_tun_device returned error {setupTunDeviceResult}");

                openconnect_vpninfo_free(_vpninfo);
                return FAILURE;
            }
        }

        _loopThread.Start();

        return SUCCESS;
    }

    private static void SetupTunHandler(void* _privdata) {
        var vpninfo = (openconnect_info*)_privdata;

        var info = Helper.AllocHGlobal<oc_ip_info>();
        var cstp = Helper.AllocHGlobal<oc_vpn_option>();
        var dtls = Helper.AllocHGlobal<oc_vpn_option>();

        openconnect_get_ip_info(vpninfo, &info, &cstp, &dtls);

        Console.WriteLine("#################################");
        Console.WriteLine("ADDR: " + Helper.PtrToStringAnsi(info->addr));
        Console.WriteLine("NETMASK: " + Helper.PtrToStringAnsi(info->netmask));
        Console.WriteLine("GATEWAY: " + Helper.PtrToStringAnsi(info->gateway_addr));
        Console.WriteLine("DNS1: " + Helper.PtrToStringAnsi(info->dns1));
        Console.WriteLine("DNS2: " + Helper.PtrToStringAnsi(info->dns2));
        Console.WriteLine("DNS3: " + Helper.PtrToStringAnsi(info->dns3));

        var include = info->split_includes;
        while (!Helper.IsNull(include)) {
            Console.WriteLine("INCLUDE: " + Helper.PtrToStringAnsi(include->route));
            include = include->next;
        }

        var opt = cstp;
        while (!Helper.IsNull(opt)) {
            Console.WriteLine("CSTP: " + Helper.PtrToStringAnsi(opt->option) + " = " + Helper.PtrToStringAnsi(opt->value));
            opt = opt->next;
        }

        opt = dtls;
        while (!Helper.IsNull(opt)) {
            Console.WriteLine("DTLS: " + Helper.PtrToStringAnsi(cstp->option) + " = " + Helper.PtrToStringAnsi(cstp->value));
            opt = opt->next;
        }

        Console.WriteLine("SetupTun: done!");
        Console.WriteLine("#################################");

        Helper.FreeHGlobal(info);
        Helper.FreeHGlobal(cstp);
        Helper.FreeHGlobal(dtls);
    }

    private record AuthFormField(String Name);
    private Int32 ProcessAuthForm(void* privdata, oc_auth_form* form) {
        String F(String? input) {
            return input switch {
                null => "<null>",
                "" => "<empty>",
                _ => input,
            };
        }
        
        var formBanner = Helper.PtrToStringAnsi(form->banner);
        var formError = Helper.PtrToStringAnsi(form->error);
        var formAction = Helper.PtrToStringAnsi(form->action);
        var formMessage = Helper.PtrToStringAnsi(form->message);
        var formAuthId = Helper.PtrToStringAnsi(form->auth_id);
        var formMethod = Helper.PtrToStringAnsi(form->method);
        
        Console.WriteLine("####################### AUTHENTICATION #######################");
        if (!String.IsNullOrWhiteSpace(formBanner)) {
            Console.WriteLine(formBanner);
        }

        if (formMethod != "POST") {
            Console.WriteLine($"DEBUG: formMethod={F(formMethod)}");
        }

        if (formAction != "/") {
            Console.WriteLine($"DEBUG: formAction={F(formAction)}");
        }

        if (formAuthId != "main") {
            Console.WriteLine($"DEBUG: formAuthId={F(formAuthId)}");
        }

        if (formError != null) {
            Console.WriteLine($"Error: {F(formError)}");
        }

        var formFields = new Dictionary<String, AuthFormField>();
        var newValues = new Dictionary<String, String>();

        // Build list of field names
        var ocField = form->opts;
        if (!Helper.IsNull(ocField)) {
            Console.WriteLine("Fields:");
        }
        
        while (!Helper.IsNull(ocField)) {
            var fieldName = Helper.PtrToStringAnsi(ocField->name);
            var fieldLabel = Helper.PtrToStringAnsi(ocField->label);
            var fieldValue = Helper.PtrToStringAnsi(ocField->_value);
            
            Console.WriteLine($" * Name: {F(fieldName)} ({ocField->type})");
            Console.WriteLine($"   Label: {F(fieldLabel)}");
            
            if (fieldValue != null) {
                Console.WriteLine($"   Value: {F(fieldValue)}");
            }

            if (ocField->flags != OC_FORM_OPT_FLAGS.NONE) {
                Console.WriteLine($"   Flags: {ocField->flags}");
            }

            Console.WriteLine();

            formFields[fieldName!] = new AuthFormField(fieldName!);
            ocField = ocField->next;
        }

        if (formFields.ContainsKey("username") && formFields.ContainsKey("password")) {
            Console.WriteLine("Auth: Detected both username and password field.");

            var messageText = String.IsNullOrWhiteSpace(formMessage)
                ? $"Enter credentials for the VPN connection to {Url}"
                : formMessage;
            
            // We've been asked about credentials. If we already have credentials,
            // assume those have been faulty.
            if (_currentCredentials != null) {
                Console.WriteLine("Auth: Marking previous credentials as incorrect.");
                _currentCredentials.Fail();

                Console.WriteLine("Auth: Showing user interface to ask for credentials...");
                var ERROR_NETWORK_ACCESS_DENIED = 65;
                _currentCredentials = AskForCredentials(messageText, ERROR_NETWORK_ACCESS_DENIED, true);
            } else {
                Console.WriteLine("Auth: Asking user for potentially stored credentials...");
                _currentCredentials = AskForCredentials(messageText);
            }

            if (_currentCredentials == null) {
                // The user did not provide any credentials.
                Console.Error.WriteLine("Auth: No credentials given, cancelling login");
                return OC_FORM_RESULT_ERR;
            }

            newValues["username"] = _currentCredentials.Username;
            newValues["password"] = _currentCredentials.Password;
        }

        if (_isFirstAuthAttempt && formFields.ContainsKey("secondary_password") && SecondaryPassword != null) {
            Console.WriteLine("Auth: Detected 'secondary_password' field, auto-filling on first login attempt.");
            newValues["secondary_password"] = SecondaryPassword;
        }

        var missingFormFields = formFields.Values
            .Where(x => !newValues.ContainsKey(x.Name))
            .ToArray();

        if (missingFormFields.Any()) {
            Console.WriteLine();
            Console.WriteLine("Press CTRL-Z to cancel the input and the connection attempt.");
            foreach (var formField in missingFormFields) {
                Console.Write($" * Enter value for field '{formField.Name}': ");

                var input = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(input)) {
                    return OC_FORM_RESULT_ERR;
                }

                newValues[formField.Name] = input;
            }

            Console.WriteLine();
        }

        ocField = form->opts;
        while (!Helper.IsNull(ocField)) {
            var fieldName = Helper.PtrToStringAnsi(ocField->name);
            var value = newValues[fieldName!];
            openconnect_set_option_value(ocField, value);

            ocField = ocField->next;
        }

        Console.WriteLine("Auth: Sending credentials to server.");
        _isFirstAuthAttempt = false;
        return OC_FORM_RESULT_OK;
    }

    private void ReportProgress(void* privdata, Int32 level, Char* formatPtr, void* vaList) {
        var vaListReader = new VaListReader(&vaList);
        ReportProgress(privdata, level, formatPtr, vaListReader);
    }
    
    private void ReportProgress(void* privdata, Int32 level, Char* formatPtr, VaListReader vaListReader) {
        // Unclear naming; the numeric values are lower for severe log levels
        // Choosing PRG_INFO (1) means we should discard PRG_DEBUG (2) and
        // PRG_TRACE (3).
        if (MinLoggingLevel < level) {
            return;
        }

        var format = Helper.PtrToStringAnsi(formatPtr);
        if (format == null) {
            return;
        }

        var message = PrintfFormatter.Format(format, vaListReader);
        if (level == 0) {
            Console.Error.WriteLine($"VPN: [{level}] {message.Trim()}");
        } else {
            Console.WriteLine($"VPN: [{level}] {message.Trim()}");
        }
    }

    private void MainLoop() {
        using (ConsoleTitle.Change($"VPN: {Url}")) {
            while (!_shouldExit.IsSet) {
                var mainloopResult = openconnect_mainloop(_vpninfo!, 30, RECONNECT_INTERVAL_MIN);
                if (mainloopResult < 0) {
                    switch (mainloopResult) {
                        case -EINTR: // Response to OC_CMD_CANCEL
                        case -ECONNABORTED: // Response to OC_CMD_DETACH
                            return;
                        case -EPIPE:
                            Console.Error.WriteLine("openconnect_mainloop returned EPIPE: remote end explicitly terminated the session.");

                            return;
                        case -EPERM:
                            Console.Error.WriteLine("openconnect_mainloop returned EPERM: gateway sent 401 Unauthorized");

                            return;
                        default:
                            Console.Error.WriteLine($"openconnect_mainloop returned {mainloopResult}, disconnected?");

                            return;
                    }
                }
            }
        }
    }

    public void Disconnect() {
        _shouldExit.Set();

        if (_cmd_fd != INVALID_SOCKET) {
            var bytesSent = send(_cmd_fd, OC_CMD_CANCEL, 1, 0);
            if (bytesSent < 0) {
                Console.Error.WriteLine($"send returned error {bytesSent}");
            }

            _cmd_fd = INVALID_SOCKET;
        }

        _loopThread.Join();

        if (_vpninfo != null) {
            openconnect_vpninfo_free(_vpninfo);
            _vpninfo = null;
        }

        _hasDisconnected.Set();
    }

    public void WaitForDisconnect() {
        _hasDisconnected.Wait();
    }

    public Credentials? AskForCredentials(String messageText, Int32 previousError = 0, Boolean forceShowUI = false) {
        var credReq = new CREDUI_INFOW {
            pszCaptionText = "VPN credentials",
            pszMessageText = messageText,
        };

        credReq.cbSize = Marshal.SizeOf(credReq);

        var performSave = false;
        var shouldConfirm = true;

        var targetName = "VPN: " + Url;
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
                $"VPN: {Url}",
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

    internal class Credentials {
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
                CredUIConfirmCredentialsW(_targetName, false);
            }
        }

        public void Success() {
            if (_shouldConfirm) {
                CredUIConfirmCredentialsW(_targetName, true);
            }
        }
    }
}