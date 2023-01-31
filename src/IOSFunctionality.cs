using System;

namespace ConnectToUrl; 

internal unsafe delegate void Logger(void* privdata, Int32 level, Char* formatted);

internal interface IOSFunctionality {
    /// <summary>
    ///   Determine if we can find an installation of OpenConnect.
    /// </summary>
    Boolean CheckForOpenConnectInstallation();

    OpenConnect.openconnect_progress_vfn CreateOpenConnectLogger(Logger callback);
}