using System;
using System.Runtime.InteropServices;

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
        /* SOCKET */ IntPtr s,
        /* long */ Int32 cmd,
        /* u_long* */ UInt32* argp
    );

    [DllImport("ws2_32.dll", EntryPoint = "send", SetLastError = true)]
    public static extern unsafe Int32 send(
        /* SOCKET */ IntPtr s,
        /* const char* */ Char* buf,
        Int32 len,
        Int32 flags
    );
}