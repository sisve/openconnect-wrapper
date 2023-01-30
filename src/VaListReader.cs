using System;
using System.Runtime.InteropServices;

namespace ConnectToUrl;

/// <summary>
///   Targets a _specific_ va_list instance and allows us to read arguments from
///   it.
///
///   I've been unable to find any documentation on how the va_list is passed,
///   and this implementation is based on guesses, inspected memory, more
///   guesses, and lots of frustration.
///
///   Assuming the caller signature `void log(const char* format, ...)`, then
///   the C# signature _could_ be `void log(string format, void* va_list)` where
///   we get a pointer to the first entry on the list. Reading this entry would
///   get us the first item, but we want to know where the pointer/parameter
///   value itself is stored, so we need `new VaListReader(&amp;va_list)` to get
///   an void**, a pointer to where the pointer to the value is.
///
///   It's a requirement to annotate the receipient of the original va_list, the
///   method that the native caller invokes, with UnmanagedCallersOnlyAttribute.
///   This simplifies the memory layout of the stack, all va_list entries are
///   sequential like an array. Skipping the attribute means that the runtime
///   will introduce more bytes, probably a guard frame or metadata about the
///   invocation, between the first item and the rest of the items in the list.
///
///   [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
///   private static void Callback(string format, void* va_list)
/// </summary>
internal unsafe class VaListReader : IPrintfValueProvider {
    private readonly IntPtr _ptr;

    private Int32 _offset;
    private IntPtr CurrentPosition => _ptr + _offset;

    public VaListReader(void** ptr) {
        _ptr = new IntPtr(ptr);

        //Console.WriteLine("PRINTING FROM READER");
        //MemoryDebug.Print(_firstPtr, size: 0x080);
        //Console.WriteLine("-------------------------------------------------------");
    }

    public Int32 ReadInt32() {
        var var = Marshal.ReadInt32(CurrentPosition);

        // Int32 seem to take up 64 bits, which happen
        // to be IntPtr.Size when this was written.
        _offset += sizeof(IntPtr);

        return var;
    }

    public Int64 ReadInt64() {
        var var = Marshal.ReadInt64(CurrentPosition);

        // Assumption: everything is taking up IntPtr.Size.
        // This happen to be the same as sizeof(Int64) when
        // this was written.
        _offset += sizeof(IntPtr);

        return var;
    }

    public String? ReadStringAnsi() {
        var ptr = Marshal.ReadIntPtr(CurrentPosition);
        _offset += IntPtr.Size;

        var str = Marshal.PtrToStringAnsi(ptr);
        return str;
    }

    public String? ReadStringUnicode() {
        var ptr = Marshal.ReadIntPtr(CurrentPosition);
        _offset += IntPtr.Size;

        var str = Marshal.PtrToStringUni(ptr);
        return str;
    }
}