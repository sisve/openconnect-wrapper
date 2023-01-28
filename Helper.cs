using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ConnectToUrl;

internal static class Helper {
    public static unsafe T* AllocHGlobal<T>() where T : unmanaged {
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
        Marshal.StructureToPtr(new T(), ptr, false);
        return (T*)ptr;
    }

    public static unsafe void FreeHGlobal(void* ptr) {
        Marshal.FreeHGlobal(new IntPtr(ptr));
    }

    public static unsafe Boolean IsNull(void* ptr) {
        return new IntPtr(ptr) == IntPtr.Zero;
    }

    public static Int16 MakeWord(Byte low, Byte high) {
        return (Int16)(high << 8 | low);
    }

    public static unsafe String? PtrToStringAnsi(Char* ch) {
        if (IsNull(ch)) {
            return null;
        }

        var ptr = new IntPtr(ch);
        return Marshal.PtrToStringAnsi(ptr);
    }

    [return: NotNull]
    public static unsafe Char* StringToHGlobalAnsi(String value) {
        var ptr = Marshal.StringToHGlobalAnsi(value);
        var voidPtr = ptr.ToPointer();
        var charPtr = (Char*)voidPtr;
        return charPtr!;
    }
}