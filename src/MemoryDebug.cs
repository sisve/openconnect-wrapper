using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ConnectToUrl;

internal static class MemoryDebug {
    public static unsafe void Print(void* start, Int32 size = 0x40) {
        Print(new IntPtr(start), size);
    }

    public static void Print(IntPtr start, Int32 size = 0x40) {
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
}