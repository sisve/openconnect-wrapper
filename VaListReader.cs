using System;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
///   It _seems_ that the first entry in the va_list is stored at the expected
///   memory location. Any additional entries, however, are stored in another
///   location. A debug compilation offsets the rest 140 bytes (18 * IntPtr.Size),
///   while a release compilation offsets them at 112 bytes (14 * IntPtr.Size).
///   It is not clear what these 140 / 112 bytes contains, or why the
///   compilation settings would affact it.
///
///   This has been tested on Windows 10 64-bit, in both debug and release
///   builds.
/// </summary>
internal unsafe class VaListReader : IPrintfValueProvider {
    private readonly IntPtr _firstPtr;
    private readonly IntPtr _restPtr;

    private Int32 _offset;

    private IntPtr CurrentPosition => _offset == 0
        ? _firstPtr
        : _restPtr + _offset;

    public VaListReader(void** ptr) {
        _firstPtr = new IntPtr(ptr);

        // The offset is 18 and 14 IntPtr.Size. We use 17 and 13 here to place
        // the _restPtr just before the first entry (in reality, the second
        // item in the va_list). We will move to the correct position once we
        // read the first va_list item, and increase _offset, which places us
        // one step further ahead than these values.
#if DEBUG
        var offset = 17 * IntPtr.Size;
#else
        var offset = 13 * IntPtr.Size;
#endif

        _restPtr = new IntPtr(ptr) + offset;

        // OutputDebug(_firstPtr, size: 0x100);
        // OutputDebug(_restPtr, size: 0x040);
        // Console.WriteLine($"offset = {offset:X4}");
        // Console.WriteLine("-------------------------------------------------------");
    }

    public void OutputDebugIntegers() {
        OutputDebug(0x10);

        Console.WriteLine("VaListReader: Int16: " + Marshal.ReadInt16(CurrentPosition));
        Console.WriteLine("              Int32: " + Marshal.ReadInt32(CurrentPosition));
        Console.WriteLine("              Int64: " + Marshal.ReadInt64(CurrentPosition));
    }

    public void OutputDebug(Int32 size = 0x40) {
        OutputDebug(CurrentPosition, size);
    }

    public void OutputDebug(IntPtr start, Int32 size = 0x40) {
        Console.WriteLine();
        Console.WriteLine($"DEBUG: Starting printing at {start:X8}");

        var bytesDebug = new Byte[size];
        Marshal.Copy(start, bytesDebug, 0, bytesDebug.Length);

        if (Environment.GetEnvironmentVariable("COMPlus_legacyCorruptedStateExceptionsPolicy") != "1") {
            Console.WriteLine("DEBUG: If the process crashes, try setting the environment variable COMPlus_legacyCorruptedStateExceptionsPolicy=1");
        }

        for (var rowOffset = 0; rowOffset < bytesDebug.Length; rowOffset += 0x10) {
            var rowStartAt = start.ToInt64() + rowOffset;
            Console.Write($"DEBUG: {rowStartAt:X8} (+{rowOffset:X4})   ");

            for (var colOffset = 0; colOffset < 0x10; colOffset++) {
                if (colOffset % 8 == 0) {
                    Console.Write("  ");
                }

                var b = bytesDebug[rowOffset + colOffset];
                Console.Write($"{b:X2} ");
            }

            Console.Write("  ");

            String Clean(String? input) {
                return Regex.Replace(input ?? "", @"[^a-zA-Z0-9\.\-]", ".").Trim();
            }

            void TryOutputPtr(Int32 offset) {
                var dataStart = start + rowOffset + offset;
                var ptr = Marshal.ReadIntPtr(dataStart);
                var diff = (ptr - dataStart).ToInt64();
                if (-1024 < diff && diff < 1024) {
                    if (diff > 0) {
                        Console.Write($"(PTR:+{diff:X}) ");
                    } else if (diff == 0) {
                        Console.Write("(PTR:same) ");
                    } else {
                        Console.Write($"(PTR:-{-diff:X} ");
                    }
                }
            }

            void TryOutputString(Int32 offset) {
                var dataStart = start + rowOffset + offset;
                var ptr = Marshal.ReadIntPtr(dataStart);

#if true
                try {
                    var str = Marshal.PtrToStringAnsi(dataStart);
                    if (str != null) {
                        str = Clean(str);

                        switch (str.Length) {
                            case > 20:
                                Console.Write($"({offset}:\"{str[..17]}...\") ");
                                break;
                            default:
                                Console.Write($"({offset}:\"{str}\") ");
                                break;
                        }
                    }
                } catch (Exception) {
                    // ReSharper disable once EmptyGeneralCatchClause
                }
#endif

                try {
                    var str = Marshal.PtrToStringAnsi(ptr);
                    if (str != null) {
                        str = Clean(str);

                        switch (str.Length) {
                            case > 20:
                                Console.Write($"({offset}:->\"{str[..17]}...\") ");
                                break;
                            default:
                                Console.Write($"({offset}:->\"{str}\") ");
                                break;
                        }
                    }
                } catch (Exception) {
                    // ReSharper disable once EmptyGeneralCatchClause
                }
            }

            TryOutputPtr(0);
            TryOutputString(0);
            Console.Write("  |  ");
            TryOutputPtr(8);
            TryOutputString(8);
            Console.WriteLine();
        }

        Console.WriteLine();
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