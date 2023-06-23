using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace ConnectToUrl.Windows;

[SupportedOSPlatform("Windows")]
internal class WindowsFunctionality : IOSFunctionality {
    private delegate void register_managed_logger_callback(Logger callback);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, String lpProcName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern Boolean FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr LoadLibrary(String lpFileName);

    public Boolean Init() {
        // init winsock
        var wsaResult = Winsock2.WSAStartup(Helper.MakeWord(1, 1), out _);
        if (wsaResult != 0) {
            Console.Error.WriteLine($"WSAStartup failed with {wsaResult}");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-wsastartup");
            return false;
        }

        return true;
    }

    public Boolean VerifyRequirements() {
        return CheckForTAPWindows() &&
               CheckForOpenConnectInstallation();
    }

    private Boolean CheckForTAPWindows() {
        var dir = @"C:\Program Files\TAP-Windows";
        if (!Directory.Exists(dir)) {
            Console.Error.WriteLine($"Missing directory {dir}, have you installed TAP-Windows?");
            return false;
        }

        return true;
    }

    private Boolean CheckForOpenConnectInstallation() {
        var dllDirectory = @"C:\Program Files\OpenConnect";
        var dllPath = Path.Combine(dllDirectory, "libopenconnect-5.dll");
        if (!File.Exists(dllPath)) {
            Console.Error.WriteLine($"Missing file {dllPath}, have you installed OpenConnect?");
            return false;
        }

        // Make [DllImport] load libopenconnect from dllDirectory.
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);

        return true;
    }

    public Boolean HasPermissions() {
        using (var identity = WindowsIdentity.GetCurrent()) {
            var principal = new WindowsPrincipal(identity);
            var isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            if (isAdministrator) {
                return true;
            }
        }

        Console.Error.WriteLine("You do not have enough permissions. Try running your Terminal,");
        Console.Error.WriteLine("Command Prompt, or shortcut, as Administrator.");
        return false;
    }

    public OpenConnect.openconnect_progress_vfn CreateOpenConnectLogger(Logger callback) {
        var libHandle = LoadLibrary(Path.Combine(AppContext.BaseDirectory, "Windows", "libnative.x64.dll"));
        if (libHandle == IntPtr.Zero) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var openconnectCallbackHandle = GetProcAddress(libHandle, "openconnect_logger_callback");
        if (openconnectCallbackHandle == IntPtr.Zero) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var registerCallbackHandle = GetProcAddress(libHandle, "register_managed_logger_callback");
        if (registerCallbackHandle == IntPtr.Zero) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var registerCallback = Marshal.GetDelegateForFunctionPointer<register_managed_logger_callback>(registerCallbackHandle);
        registerCallback(callback);

        return Marshal.GetDelegateForFunctionPointer<OpenConnect.openconnect_progress_vfn>(openconnectCallbackHandle);
    }

    public unsafe Boolean SetSocketNonblocking(Int32 fd) {
        var mode = 0u; // blocking
        var FIONBIO = -2147195266;
        var ioctlResult = Winsock2.ioctlsocket(new IntPtr(fd), FIONBIO, &mode);
        if (ioctlResult != 0) {
            Console.Error.WriteLine($"ioctlsocket returned error {ioctlResult}");
            Console.Error.WriteLine("Check https://learn.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-ioctlsocket");
            return false;
        }

        return true;
    }

    public unsafe Int64 send(Int32 fd, Char* buffer, UInt32 length) {
        var bytesSent = Winsock2.send(fd, buffer, (Int32)length, 0);
        if (bytesSent < 0) {
            Console.Error.WriteLine($"send returned error {bytesSent}");
            return bytesSent;
        }

        return bytesSent;
    }
}