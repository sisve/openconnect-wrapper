using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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

    public Boolean CheckForOpenConnectInstallation() {
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
}