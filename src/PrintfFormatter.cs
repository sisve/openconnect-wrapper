using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace ConnectToUrl;

/// <summary>
///   A somewhat limited printf formatter. The goal is to support the formats
///   used by the openconnect library. Anything else is out-of-scope.
/// </summary>
/// <remarks>
///   See https://cplusplus.com/reference/cstdio/printf/
/// </remarks>
internal static class PrintfFormatter {
    public static String Format(String format, IPrintfValueProvider reader) {
        if (!format.Contains('%')) {
            // There are no format specifications
            return format;
        }

        var result = new StringBuilder();

        // Once we hit any type of problem we should not longer use the reader.
        // If we instead try to ignore the problem, and continue with the next
        // format specifier, then we risk a mismatch between the argument order
        // provided by the reader and the format specifiers in the format string.
        var isFaulted = false;

        var span = format.AsSpan();
        while (true) {
            if (isFaulted) {
                // We're faulted, and shouldn't process anything further.
                result.Append(span);
                break;
            }

            var nextStart = span.IndexOf('%');
            if (nextStart == -1) {
                // There are no more % in the format.
                result.Append(span);
                break;
            }

            if (nextStart > 0) {
                // There's text before the next %
                result.Append(span[..nextStart]);
                span = span[nextStart..];
            }

            if (!PrintfFormatSpecifier.TryParse(span, out var specifier)) {
                // Failed to parse.
                isFaulted = true;
                result.Append(span[0]);
                span = span[1..];
                continue;
            }

            var specifierLength = specifier.Value.Length;
            span = span[specifierLength..];

            try {
                var specifierResult = FormatSpecifier(specifier, reader);
                result.Append(specifierResult);
            } catch (NotSupportedException ex) {
                Console.Error.WriteLine("printf: " + ex.Message);
                isFaulted = true;
                result.Append(specifier.Value);
                continue;
            }
        }

        return result.ToString();
    }

    private static String FormatSpecifier(PrintfFormatSpecifier specifier, IPrintfValueProvider reader) {
        if (specifier.PrecisionFromArgument) {
            throw new NotSupportedException($"Not supported: PrecisionFromArgument in '{specifier.Value}'");
        }

        if (specifier.WidthFromArgument) {
            throw new NotSupportedException($"Not supported: WidthFromArgument in '{specifier.Value}'");
        }

        [DoesNotReturn]
        Exception LengthNotSupported() {
            throw new NotSupportedException($"Not supported: type='{specifier.Type}' length='{specifier.Length}' in '{specifier.Value}'");
        }

        void AssertNoFlagsSpecified() {
            if (specifier.Flags != PrintfFlags.None) {
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' flags={specifier.Flags} in '{specifier.Value}'");
            }
        }

        void AssertNoWidthSpecified() {
            if (specifier.Width != null) {
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' width='{specifier.Width}'in '{specifier.Value}'");
            }
        }

        void AssertNoPrecisionSpecified() {
            if (specifier.Precision != null) {
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' precision='{specifier.Precision}'in '{specifier.Value}'");
            }
        }

        void AssertNoLengthSpecified() {
            if (specifier.Length != null) {
                throw LengthNotSupported();
            }
        }

        // Type sizes @ https://learn.microsoft.com/en-us/windows/win32/winprog64/the-new-data-types
        // Docs for printf @ https://cplusplus.com/reference/cstdio/printf/
        //
        // size_t: "size of any object in bytes" -> unsigned long long int?
        // ptrdiff_t: "the result of any valid pointer subtraction operation"
        //
        // The "in source" comments refer to openconnect's source code

        switch (specifier.Type) {
            case 'd':
            case 'i': {
                // signed decimal integer
                // In source: %d, %ld
                AssertNoFlagsSpecified();
                AssertNoPrecisionSpecified();

                IFormattable value = specifier.Length switch {
                    null => reader.ReadInt32(), // int
                    "hh" => throw LengthNotSupported(), // signed char
                    "h" => throw LengthNotSupported(), // short int
                    "l" => reader.ReadInt64(), // long int
                    "ll" => throw LengthNotSupported(), // long long int
                    "j" => throw LengthNotSupported(), // intmax_t
                    "z" => reader.ReadInt64(), // size_t
                    "t" => throw LengthNotSupported(), // ptrdiff_t
                    _ => throw LengthNotSupported(),
                };

                var formatString = "D";
                if (specifier.Width != null) {
                    formatString += specifier.Width.Value;
                }

                return value.ToString(formatString, CultureInfo.InvariantCulture);
            }
            case 'u':
            case 'o':
            case 'x':
            case 'X': {
                // u: unsigned decimal integer
                // o: unsigned octal
                // x: unsigned hexadecimal lower-case
                // X: unsigned hexadecimal upper-case
                // In source: %u, %zu
                // In source: %x,  %lx %llx, %02x, %04x, %08x
                // Not supported: %llx
                AssertNoFlagsSpecified();
                AssertNoPrecisionSpecified();

                // We can only read signed values from the reader. Change
                // this when the reader supports unsigned data types.
                IFormattable value = specifier.Length switch {
                    null => reader.ReadInt32(), // unsigned int
                    "hh" => throw LengthNotSupported(), // unsigned signed char
                    "h" => throw LengthNotSupported(), // unsigned short int
                    "l" => reader.ReadInt64(), // unsigned long int
                    "ll" => throw LengthNotSupported(), // unsigned long long int
                    "j" => throw LengthNotSupported(), // uintmax_t
                    "z" => reader.ReadInt64(), // size_t
                    "t" => throw LengthNotSupported(), // ptrdiff_t
                    _ => throw LengthNotSupported(),
                };

                var formatString = specifier.Type switch {
                    'u' => "D",
                    'x' => "x",
                    'X' => "X",
                    _ => "D",
                };

                if (specifier.Width != null) {
                    formatString += specifier.Width.Value;
                }

                return value.ToString(formatString, CultureInfo.InvariantCulture);
            }

            case 'f':
            case 'F':
                // decimal floating point, lowercase and uppercase(?)
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 'e':
            case 'E':
                // scientific notation, lowercase and uppercase
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 'g':
            case 'G':
                // shortest of %e, %f, %E, %F
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 'a':
            case 'A':
                // hexadecimal flaoting point, lowercase and uppercase
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 'c':
                // character
                // In source: %c
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 's': {
                // string
                // In source: %s
                AssertNoFlagsSpecified();
                AssertNoWidthSpecified();
                AssertNoPrecisionSpecified();

                if (specifier.Length == null) {
                    // char*
                    return reader.ReadStringAnsi() ?? "";
                }

                if (specifier.Length == "l") {
                    // wchar_t*
                    return reader.ReadStringUnicode() ?? "";
                }

                throw LengthNotSupported();
            }

            case 'S': {
                // "(Not in C99, but in SUSv2.) Synonym for ls. Don't use."
                // In source: %S
                AssertNoFlagsSpecified();
                AssertNoWidthSpecified();
                AssertNoLengthSpecified();
                AssertNoPrecisionSpecified();

                // wchar_t*
                return reader.ReadStringUnicode() ?? "";
            }

            case 'p':
                // pointer address
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case 'n':
                // special: store number of characters written so far in the argument
                // In source: none
                throw new NotSupportedException($"Not supported: type='{specifier.Type}' in '{specifier.Value}'");

            case '%':
                return "%";

            default:
                throw new NotSupportedException($"Unknown specifier type '{specifier.Type}'");
        }
    }
}

internal interface IPrintfValueProvider {
    public Int32 ReadInt32();
    public Int64 ReadInt64();
    public String? ReadStringAnsi();
    public String? ReadStringUnicode();
}