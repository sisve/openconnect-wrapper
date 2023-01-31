using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ConnectToUrl.Windows;

[SupportedOSPlatform("Windows")]
internal static class Winsock2 {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WSAData64 {
        public UInt16 wVersion;
        public UInt16 wHighVersion;
        public UInt16 iMaxSockets;
        public UInt16 iMaxUdpDg;
        public IntPtr lpVendorInfo;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
        public String szDescription;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
        public String szSystemStatus;
    }

    [DllImport("ws2_32.dll", EntryPoint = "WSAStartup", SetLastError = true)]
    public static extern Int32 WSAStartup(Int16 wVersionRequested, out WSAData64 wsaData);

    [DllImport("Ws2_32.dll", EntryPoint = "ioctlsocket")]
    public static extern unsafe Int32 ioctlsocket(
        [OriginalType("SOCKET")]
        IntPtr s,

        [OriginalType("long")]
        Int32 cmd,

        [OriginalType("u_long")]
        UInt32* argp
    );

    [DllImport("ws2_32.dll", EntryPoint = "send", SetLastError = true)]
    public static extern unsafe Int32 send(
        [OriginalType("SOCKET")]
        IntPtr s,

        [OriginalType("const char*")]
        Char* buf,

        Int32 len,
        Int32 flags
    );
}