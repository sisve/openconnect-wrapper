using System;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix.Native;

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

    public Boolean CheckPermissions() {
        var isRoot = Syscall.geteuid() == 0;
        if (isRoot) {
            return true;
        }

        Console.Error.WriteLine("You do not have enough permissions. Try running the tool with sudo.");
        return false;
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

    public Boolean SetSocketNonblocking(Int32 fd) {
        var flags = Syscall.fcntl(fd, FcntlCommand.F_GETFL);
        var newFlags = flags | (Int32)OpenFlags.O_NONBLOCK;
        var fcntlResult = Syscall.fcntl(fd, FcntlCommand.F_SETFL, newFlags);
        if (fcntlResult != 0) {
            var errno = Stdlib.GetLastError();
            var errmsg = Stdlib.strerror(errno);
            Console.Error.WriteLine($"fcntl returned error {fcntlResult}, errno={errno}, errmsg='{errmsg}'");
            return false;
        }

        return true;
    }

    public unsafe Int64 send(Int32 fd, Char* buffer, UInt32 length) {
        var bytesSent = Syscall.send(fd, buffer, length, 0);
        if (bytesSent < 0) {
            var errno = Stdlib.GetLastError();
            var errmsg = Stdlib.strerror(errno);
            Console.Error.WriteLine($"write returned error {bytesSent}, errno={errno}, errmsg='{errmsg}'");
        }

        return bytesSent;
    }
}