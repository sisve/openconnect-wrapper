using System;

namespace ConnectToUrl;

internal unsafe delegate void Logger(void* privdata, Int32 level, Char* formatted);

internal unsafe interface IOSFunctionality {
    Boolean Init() {
        return true;
    }

    /// <summary>
    ///   Determine if we can find an installation of OpenConnect.
    /// </summary>
    Boolean CheckForOpenConnectInstallation();

    OpenConnect.openconnect_progress_vfn CreateOpenConnectLogger(Logger callback);

    Boolean SetSocketNonblocking(Int32 fd);

    /// <summary>
    ///   send(...) without flags, or write(...) on some platforms
    /// </summary>
    Int64 send(Int32 fd, Char* buffer, UInt32 length);
}