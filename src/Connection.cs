﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static OpenConnect;

namespace ConnectToUrl;

/// <summary>
///   This class handles the interaction with the different openconnect_*
///   native method calls. This includes creating and configuring the
///   connection, and handle authentication.
/// </summary>
internal unsafe class Connection {
    private const Int32 SUCCESS = 0;
    private const Int32 FAILURE = 1;

    private const Int32 INVALID_SOCKET = -1;

    // https://learn.microsoft.com/en-us/cpp/c-runtime-library/errno-constants
    private const Int32 EINTR = 4;
    private const Int32 ECONNABORTED = 106;
    private const Int32 EPIPE = 32;
    private const Int32 EPERM = 1;

    private readonly ManualResetEventSlim _hasDisconnected = new ManualResetEventSlim();
    private readonly Thread _loopThread;

    // We're holding on to the delegates here, to make sure they are never
    // garbage collected while the class is still alive.
    private readonly openconnect_process_auth_form_vfn ProcessAuthFormDelegate;
    private readonly openconnect_setup_tun_vfn SetupTunDelegate;
    private readonly openconnect_progress_vfn ProgressDelegate;

    private State* _state;
    private Int32 _cmd_fd = INVALID_SOCKET;

    private Boolean _isFirstAuthAttempt = true;
    private IVpnCredentials? _currentCredentials;

    internal Connection() {
        _loopThread = new Thread(MainLoop);

        ProcessAuthFormDelegate = ProcessAuthForm;
        SetupTunDelegate = SetupTunHandler;
        ProgressDelegate = Platform.OSFunctionality.CreateOpenConnectLogger(ProgressCallback);
    }

    public String? Url { get; init; }
    public Int32 MinLoggingLevel { get; init; }
    public String? ScriptPath { get; init; }
    public String? SecondaryPassword { get; init; }

    private struct State {
        public Int32 minLoggingLevel;
        public openconnect_info* vpninfo;
    }

    internal Int32 Connect() {
        if (Url == null) {
            Console.Error.WriteLine("No Url specified.");
            return FAILURE;
        }

        if (!Platform.OSFunctionality.Init()) {
            return FAILURE;
        }

        _state = Helper.AllocHGlobal<State>();
        _state->minLoggingLevel = MinLoggingLevel;

        openconnect_info* vpninfo = null;
        try {
            // https://gitlab.com/openconnect/openconnect/-/issues/665
            // Newer servers require a user agent that starts with "AnyConnect".
            var useragent = "AnyConnect-compatible OpenConnect VPN Agent";

            vpninfo = openconnect_vpninfo_new(
                useragent,
                null,
                null,
                ProcessAuthFormDelegate,
                ProgressDelegate,
                _state
            );
        } catch (BadImageFormatException ex) {
            Console.Error.WriteLine(ex.Message);
            Console.WriteLine();
            Console.Error.WriteLine("!!!");
            Console.Error.WriteLine("!!! An error occurred when talking to OpenConnect. This error happens if you've");
            Console.Error.WriteLine("!!! installed the 32bit OpenConnect release. Verify that you installed the 64bit");
            Console.Error.WriteLine("!!! version of OpenConnect.");
            Console.Error.WriteLine("!!!");
            Console.WriteLine();
            goto failure;
        }

        _state->vpninfo = vpninfo;

        Platform.WebView?.Attach(vpninfo);

        _cmd_fd = openconnect_setup_cmd_pipe(vpninfo);
        if (_cmd_fd == INVALID_SOCKET) {
            var lastError = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"openconnect_setup_cmd_pipe returned error {lastError} when setting up cmd_fd.");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2");
            goto failure;
        }

        if (!Platform.OSFunctionality.SetSocketNonblocking(_cmd_fd)) {
            return FAILURE;
        }

        var setProtoResult = openconnect_set_protocol(vpninfo, "anyconnect");
        if (setProtoResult != 0) {
            Console.Error.WriteLine($"openconnect_set_protocol returned error {setProtoResult}");
            goto failure;
        }

        openconnect_set_setup_tun_handler(vpninfo, SetupTunDelegate);

        // Disable ipv6. This method always returns 0 before we're connected.
        _ = openconnect_disable_ipv6(vpninfo);

        // Enable perfect forward secrecy.
        openconnect_set_pfs(vpninfo, 1);

        var parseUrlResult = openconnect_parse_url(vpninfo, Url);
        if (parseUrlResult != 0) {
            Console.Error.WriteLine($"openconnect_parse_url returned error {parseUrlResult}");
            goto failure;
        }

        var setReportedOsResult = openconnect_set_reported_os(vpninfo, "win");
        if (setReportedOsResult != 0) {
            Console.Error.WriteLine($"openconnect_set_reported_os returned error {setReportedOsResult}");
            goto failure;
        }

        var optainCookieResult = openconnect_obtain_cookie(vpninfo);
        if (optainCookieResult != 0) {
            Console.Error.WriteLine($"openconnect_obtain_cookie returned error {optainCookieResult}");
            goto failure;
        }

        try {
            // Mark current credentials as working.
            _currentCredentials?.Success();
        } catch (Exception ex) {
            Console.Error.WriteLine("Failed to mark credentials as successful.");
            Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }

        var makeCstpConnectionResult = openconnect_make_cstp_connection(vpninfo);
        if (makeCstpConnectionResult != 0) {
            Console.Error.WriteLine($"openconnect_make_cstp_connection returned error {makeCstpConnectionResult}");
            goto failure;
        }

        if (ScriptPath != null) {
            var setupTunDeviceResult = openconnect_setup_tun_device(vpninfo, ScriptPath, null);
            if (setupTunDeviceResult != 0) {
                Console.Error.WriteLine($"openconnect_setup_tun_device returned error {setupTunDeviceResult}");
                Console.WriteLine();
                Console.Error.WriteLine("!!!");
                Console.Error.WriteLine("!!! A common cause for this function to fail is when there's no available");
                Console.Error.WriteLine("!!! virtual ethernet adapter for TAP-Windows to use. Make sure that you have");
                Console.Error.WriteLine("!!! enough, and read https://github.com/sisve/openconnect-wrapper/ for help");
                Console.Error.WriteLine("!!! on setting up additional adapters.");
                Console.Error.WriteLine("!!!");
                Console.WriteLine();

                goto failure;
            }
        }

        _loopThread.Start();
        return SUCCESS;

        failure:
        if (vpninfo != null) {
            openconnect_vpninfo_free(vpninfo);
        }

        Helper.FreeHGlobal(ref _state);
        return FAILURE;
    }

    private static void SetupTunHandler(void* _privdata) {
        var privdata = (State*)_privdata;
        var vpninfo = privdata->vpninfo;

        oc_ip_info* info = null;
        oc_vpn_option* cstp = null;
        oc_vpn_option* dtls = null;

        // openconnect_get_ip_info always return 0
        _ = openconnect_get_ip_info(vpninfo, &info, &cstp, &dtls);

        Console.WriteLine("#################################");
        Console.WriteLine("ADDR: " + Helper.PtrToStringAnsi(info->addr));
        Console.WriteLine("NETMASK: " + Helper.PtrToStringAnsi(info->netmask));
        Console.WriteLine("GATEWAY: " + Helper.PtrToStringAnsi(info->gateway_addr));
        Console.WriteLine("DNS1: " + Helper.PtrToStringAnsi(info->dns1));
        Console.WriteLine("DNS2: " + Helper.PtrToStringAnsi(info->dns2));
        Console.WriteLine("DNS3: " + Helper.PtrToStringAnsi(info->dns3));

        var include = info->split_includes;
        while (include != null) {
            Console.WriteLine("INCLUDE: " + Helper.PtrToStringAnsi(include->route));
            include = include->next;
        }

        var opt = cstp;
        while (opt != null) {
            Console.WriteLine("CSTP: " + Helper.PtrToStringAnsi(opt->option) + " = " + Helper.PtrToStringAnsi(opt->value));
            opt = opt->next;
        }

        opt = dtls;
        while (opt != null) {
            Console.WriteLine("DTLS: " + Helper.PtrToStringAnsi(cstp->option) + " = " + Helper.PtrToStringAnsi(cstp->value));
            opt = opt->next;
        }

        Console.WriteLine("SetupTun: done!");
        Console.WriteLine("#################################");
    }

    private record AuthFormField(String Name, OC_FORM_OPT_TYPE Type);

    private OC_FORM_RESULT ProcessAuthForm(void* _privdata, oc_auth_form* form) {
        String F(String? input) {
            return input switch {
                null => "<null>",
                "" => "<empty>",
                _ => input,
            };
        }

        var credentialManager = Platform.CredentialManager;
        var credentialStore = Platform.CredentialStore;

        var formBanner = Helper.PtrToStringAnsi(form->banner);
        var formError = Helper.PtrToStringAnsi(form->error);
        var formAction = Helper.PtrToStringAnsi(form->action);
        var formMessage = Helper.PtrToStringAnsi(form->message);
        var formAuthId = Helper.PtrToStringAnsi(form->auth_id);
        var formMethod = Helper.PtrToStringAnsi(form->method);

        BoxVerticalMargin();
        Console.WriteLine("    ########################## AUTHENTICATION ##########################");
        BoxContent();
        if (!String.IsNullOrWhiteSpace(formBanner)) {
            BoxContent(formBanner);
            BoxContent();
        }

        if (formMethod != "POST") {
            BoxContent($"DEBUG: formMethod={F(formMethod)}");
        }

        if (formAction != "/") {
            BoxContent($"DEBUG: formAction={F(formAction)}");
        }

        if (formAuthId != "main") {
            BoxContent($"DEBUG: formAuthId={F(formAuthId)}");
        }

        if (formError != null) {
            BoxContent($"Error: {F(formError)}");
        }

        var formFields = new Dictionary<String, AuthFormField>();
        var newValues = new Dictionary<String, String>();

        // Build list of field names
        var ocField = form->opts;
        if (ocField != null) {
            BoxContent($"Fields:");
        }

        while (ocField != null) {
            var fieldName = Helper.PtrToStringAnsi(ocField->name);
            var fieldLabel = Helper.PtrToStringAnsi(ocField->label);
            var fieldValue = Helper.PtrToStringAnsi(ocField->_value);

            BoxContent($" * Name: {F(fieldName)} ({ocField->type})");
            BoxContent($"   Label: {F(fieldLabel)}");

            if (fieldValue != null) {
                BoxContent($"   Value: {F(fieldValue)}");
            }

            if (ocField->flags != OC_FORM_OPT_FLAGS.NONE) {
                BoxContent($"   Flags: {ocField->flags}");
            }

            BoxContent();

            formFields[fieldName!] = new AuthFormField(fieldName!, ocField->type);
            ocField = ocField->next;
        }

        var isFormsLogin = formFields.ContainsKey("username") && formFields.ContainsKey("password");
        if (isFormsLogin) {
            if (TryReadFromCredentialManager(out _currentCredentials)) {
                if (_currentCredentials == null) {
                    // The credential manager did not provide any credentials.
                    BoxContent("Auth: No credentials given, cancelling login");
                    return OC_FORM_RESULT.ERR;
                }

                newValues["username"] = _currentCredentials.Username;
                newValues["password"] = _currentCredentials.Password;
            } else if (_isFirstAuthAttempt && TryReadFromCredentialStore(out _currentCredentials)) {
                // This is our first attempt, and we can try to ask the credential
                // store for information.
                if (_currentCredentials != null) {
                    BoxContent("Auth: Detected 'username' and 'password' field, auto-filling on first login attempt.");
                    newValues["username"] = _currentCredentials.Username;
                    newValues["password"] = _currentCredentials.Password;
                }
            }
        }

        if (_isFirstAuthAttempt && formFields.ContainsKey("secondary_password") && SecondaryPassword != null) {
            BoxContent("Auth: Detected 'secondary_password' field, auto-filling on first login attempt.");
            newValues["secondary_password"] = SecondaryPassword;
        }

        var missingFormFields = formFields.Values
            .Where(x => !newValues.ContainsKey(x.Name))

            // The end-user cannot manually input SSO fields
            .Where(x => x.Type != OC_FORM_OPT_TYPE.SSO_USER)
            .Where(x => x.Type != OC_FORM_OPT_TYPE.SSO_TOKEN)

            // Hide hidden fields.
            .Where(x => x.Type != OC_FORM_OPT_TYPE.HIDDEN)

            .ToArray();

        if (missingFormFields.Any()) {
            BoxContent();
            BoxContent("Press CTRL-Z to cancel the input and the connection attempt.");
            BoxContent();
            foreach (var formField in missingFormFields) {
                BoxContent($" * Enter value for field '{formField.Name}': ", newLine: false);

                var input = formField.Type switch {
                    OC_FORM_OPT_TYPE.PASSWORD => ReadSecretFromConsole(),
                    _ => Console.ReadLine(),
                };

                if (String.IsNullOrWhiteSpace(input)) {
                    BoxContent();
                    BoxBorderBottom();
                    return OC_FORM_RESULT.ERR;
                }

                newValues[formField.Name] = input;
            }

            BoxContent();

            // The user has filled in all missing fields in the above loop.
            // The _currentCredentials is null since we have read it from
            // the end-user, not the credential store. We can now create
            // a credential object that can be used to save the credentials
            // for later connection attempts.
            if (_currentCredentials == null &&
                credentialStore != null &&
                newValues.TryGetValue("username", out var newUsername) &&
                newValues.TryGetValue("password", out var newPassword)) {

                BoxContent(" * Do you want to save the login credentials for future connections? [y/N] ", newLine: false);
                var shouldSaveCredentials = Console.ReadLine() is "Y" or "y";
                if (shouldSaveCredentials) {
                    _currentCredentials = credentialStore.CreateCredentials(Url!, newUsername, newPassword);
                }
            }
        }

        ocField = form->opts;
        while (ocField != null) {
            var fieldName = Helper.PtrToStringAnsi(ocField->name);
            if (newValues.TryGetValue(fieldName!, out var value)) {
                _ = openconnect_set_option_value(ocField, value);
            }

            ocField = ocField->next;
        }

        BoxBorderBottom();

        _isFirstAuthAttempt = false;
        return OC_FORM_RESULT.OK;

        void BoxContent(String content = "", Boolean newLine = true) {
            if (newLine) {
                Console.WriteLine("    ##  " + content);
            } else {
                Console.Write("    ##  " + content);
            }
        }

        void BoxVerticalMargin() {
            Console.WriteLine();
            Console.WriteLine();
        }

        void BoxBorderBottom() {
            Console.WriteLine("    ####################################################################");
            BoxVerticalMargin();
        }

        Boolean TryReadFromCredentialManager(out IVpnCredentials? result) {
            if (credentialManager == null) {
                result = null;
                return false;
            }

            BoxContent("Auth: Detected both username and password field.");

            var messageText = String.IsNullOrWhiteSpace(formMessage)
                ? $"Enter credentials for the VPN connection to {Url}"
                : formMessage;

            // We've been asked about credentials. If we already have credentials,
            // assume those have been faulty.
            if (_currentCredentials != null) {
                BoxContent("Auth: Marking previous credentials as incorrect.");
                _currentCredentials.Fail();

                BoxContent("Auth: Showing user interface to ask for credentials...");
                result = credentialManager.ForceAskForCredentials(Url!, messageText);
                return true;
            } else {
                BoxContent("Auth: Asking user for potentially stored credentials...");
                result = credentialManager.AskForCredentials(Url!, messageText);
                return true;
            }
        }

        Boolean TryReadFromCredentialStore(out IVpnCredentials? result) {
            if (credentialStore == null) {
                result = null;
                return false;
            }

            // This is our first attempt, and we can try to ask the credential
            // store for information.
            result = credentialStore.ReadCredentials(Url!);
            return true;
        }
    }

    private static String? ReadSecretFromConsole() {
        var sb = new StringBuilder();
        var rng = new Random();
        var starCounts = new Stack<Int32>();

        while (true) {
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Enter) {
                // Console.ReadLine() adds a newline at the end.
                Console.WriteLine();
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace) {
                if (sb.Length > 0) {
                    // Move back, and clear out the *.
                    var prevStarCount = starCounts.Pop();
                    var clearStr =
                        new String('\b', prevStarCount) +
                        new String('\0', prevStarCount) +
                        new String('\b', prevStarCount);

                    Console.Write(clearStr);
                    sb.Length--;
                }

                continue;
            }

            if (sb.Length == 0 && keyInfo is { Key: ConsoleKey.Z, Modifiers: ConsoleModifiers.Control }) {
                // Console.ReadLine() returns null on Ctrl+Z, if it is the first character in the input
                Console.WriteLine();
                return null;
            }

            var starCount = rng.Next(1, 4);
            starCounts.Push(starCount);
            Console.Write(new String('*', starCount));
            sb.Append(keyInfo.KeyChar);
        }

        return sb.ToString();
    }

    private static void ProgressCallback(void* _privdata, Int32 level, Char* formatted) {
        var privdata = (State*)_privdata;

        // Unclear naming; the numeric values are lower for severe log levels
        // Choosing PRG_INFO (1) means we should discard PRG_DEBUG (2) and
        // PRG_TRACE (3).
        if (privdata->minLoggingLevel < level) {
            return;
        }

        var message = Helper.PtrToStringAnsi(formatted);
        if (message != null) {
            if (level == 0) {
                Console.Error.WriteLine($"VPN: [{level}] {message.Trim()}");
            } else {
                Console.WriteLine($"VPN: [{level}] {message.Trim()}");
            }
        }
    }

    private void MainLoop() {
        using (ConsoleTitle.Change($"VPN: {Url}")) {
            var mainloopResult = openconnect_mainloop(_state->vpninfo, 30, RECONNECT_INTERVAL_MIN);
            switch (mainloopResult) {
                case 0:
                    // The mainloop exited after an OC_CMD_PAUSE. This is
                    // used to pause and resume vpn connections. We do not
                    // support such functionality.
                    break;

                case -EINTR: // Response to OC_CMD_CANCEL
                case -ECONNABORTED: // Response to OC_CMD_DETACH
                    break;
                case -EPIPE:
                    Console.Error.WriteLine("openconnect_mainloop returned EPIPE: remote end explicitly terminated the session.");
                    break;
                case -EPERM:
                    Console.Error.WriteLine("openconnect_mainloop returned EPERM: gateway sent 401 Unauthorized");
                    break;
                default:
                    Console.Error.WriteLine($"openconnect_mainloop returned {mainloopResult}, disconnected?");
                    break;
            }

            // Free up resources
            _cmd_fd = INVALID_SOCKET;

            if (_state->vpninfo != null) {
                openconnect_vpninfo_free(_state->vpninfo);
                _state->vpninfo = null;
            }

            if (_state != null) {
                Helper.FreeHGlobal(ref _state);
                _state = null;
            }

            // Set _hasDisconnected so that anyone using WaitForDisconnect
            // knows that we're not longer connected.
            _hasDisconnected.Set();
        }
    }

    public void Disconnect() {
        if (_cmd_fd == INVALID_SOCKET) {
            Console.Error.WriteLine("The command socket has been destroyed.");
            return;
        }

        Platform.OSFunctionality.send(_cmd_fd, OC_CMD_CANCEL, 1);

        _loopThread.Join();
    }

    public void WaitForDisconnect() {
        _hasDisconnected.Wait();
    }
}