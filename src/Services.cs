using System;
using System.Runtime.InteropServices;
using ConnectToUrl.Windows;

namespace ConnectToUrl; 

internal static class Services {
    static Services() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            OSFunctionality = new WindowsFunctionality();
            CredentialManager = new WindowsCredentialManager();
        } else {
            throw new PlatformNotSupportedException();
        }
    }
    
    internal static IOSFunctionality OSFunctionality { get; }
    internal static ICredentialManager CredentialManager { get; }
}