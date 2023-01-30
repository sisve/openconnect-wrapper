using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConnectToUrl.OSX;

internal class OSXFunctionality : IOSFunctionality {
    private delegate void register_managed_logger_callback(Logger callback);

    [DllImport("libdl", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr dlsym(IntPtr handle, String symbol);

    [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
    private static extern Boolean dlclose(IntPtr handle);

    public const Int32 RTLD_LAZY = 1;
    [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr dlopen(String lpFileName, Int32 flags);

    [DllImport("libdl", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern String dlerror();

    public Boolean CheckForOpenConnectInstallation() {
        // TODO: Work in progress!
        Console.WriteLine("TODO: CheckForOpenConnectInstallation");
        return true;
    }

    public OpenConnect.openconnect_progress_vfn CreateOpenConnectLogger(Logger callback) {
        var libHandle = dlopen(Path.Combine(AppContext.BaseDirectory, "OSX", "libnative.x64.dylib"), RTLD_LAZY);
        if (libHandle == IntPtr.Zero) {
            throw new Exception(dlerror());
        }

        var openconnectCallbackHandle = dlsym(libHandle, "openconnect_logger_callback");
        if (openconnectCallbackHandle == IntPtr.Zero) {
            throw new Exception(dlerror());
        }
        
        var registerCallbackHandle = dlsym(libHandle, "register_managed_logger_callback");
        if (registerCallbackHandle == IntPtr.Zero) {
            throw new Exception(dlerror());
        }

        var registerCallback = Marshal.GetDelegateForFunctionPointer<register_managed_logger_callback>(registerCallbackHandle);
        registerCallback(callback);

        return Marshal.GetDelegateForFunctionPointer<OpenConnect.openconnect_progress_vfn>(openconnectCallbackHandle);
    }
}