using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ConnectToUrl;

/// <summary>
///   Holds information about a format specified used by printf. These are in
///   the format `%[flags][width][.precision][length]type`.
/// </summary>
/// <remarks>
///   See https://cplusplus.com/reference/cstdio/printf/
/// </remarks>
public class PrintfFormatSpecifier {
    public PrintfFormatSpecifier(String value, Char type, PrintfFlags flags, Int32? width, Boolean widthFromArgument, Int32? precision, Boolean precisionFromArgument, String? length) {
        Value = value;
        Type = type;

        Flags = flags;
        Width = width;
        WidthFromArgument = widthFromArgument;
        Precision = precision;
        PrecisionFromArgument = precisionFromArgument;
        Length = length;
    }

    public String Value { get; }
    public Char Type { get; }
    public PrintfFlags Flags { get; }

    public Int32? Width { get; }
    public Boolean WidthFromArgument { get; }

    public Int32? Precision { get; }
    public Boolean PrecisionFromArgument { get; }

    public String? Length { get; }

    public static Boolean TryParse(ReadOnlySpan<Char> span, [NotNullWhen(true)] out PrintfFormatSpecifier? result) {
        var state = new ParseState();
        var idx = 1;

        // Flags
        while (idx < span.Length - 1) {
            switch (span[idx]) {
                case '-':
                    state.Flags |= PrintfFlags.LeftJustify;
                    break;

                case '+':
                    state.Flags |= PrintfFlags.ForceSign;
                    break;

                case '0':
                    state.Flags |= PrintfFlags.ZeroPad;
                    break;

                case '#':
                    state.Flags |= PrintfFlags.Alternate;
                    break;

                case ' ':
                    state.Flags |= PrintfFlags.SpaceIfMissingSign;
                    break;

                default:
                    goto afterFlags;
            }

            idx++;
        }

        afterFlags:

        // Width
        while (idx < span.Length - 1) {
            switch (span[idx]) {
                case '-':
                    state.Width.Add(span[idx]);
                    idx++;
                    continue;
                case >= '0' and <= '9':
                    state.Width.Add(span[idx]);
                    idx++;
                    continue;
            }

            break;
        }

        // Precision
        if (span[idx] == '.') {
            idx++;

            if (span[idx] == '*') {
                state.Precision.Add(span[idx]);
                idx++;
                goto afterPrecision;
            }

            while (idx < span.Length - 1) {
                if ('0' <= span[idx] && span[idx] <= '9') {
                    state.Precision.Add(span[idx]);
                    idx++;
                    continue;
                }

                break;
            }
        }

        afterPrecision:

        // Size:
        while (idx < span.Length - 1) {
            switch (span[idx]) {
                case 'h':
                case 'l':
                case 'j':
                case 'z':
                case 't':
                case 'L':
                    state.Length.Add(span[idx]);
                    idx++;
                    break;
                default:
                    goto afterSize;
            }
        }

        afterSize:

        // Specifier
        switch (span[idx]) {
            case 'd':
            case 'i':
            case 'o':
            case 'x':
            case 'X':
            case 'f':
            case 'F':
            case 'e':
            case 'E':
            case 'g':
            case 'G':
            case 'a':
            case 'A':
            case 'c':
            case 's':
            case 'p':
            case 'n':
            case '%':
                state.Type = span[idx];
                idx++;
                break;
        }

        if (state.Type == default) {
            result = default;
            return false;
        }

        var value = new String(span[..idx]);

        var width = new Int32?();
        var widthFromArguments = false;
        if (state.Width is ['*']) {
            widthFromArguments = true;
        } else if (state.Width.Count > 0) {
            var str = new String(state.Width.ToArray());
            if (!Int32.TryParse(str, out var val)) {
                throw new NotSupportedException($"Unknown width argument '{str}' in '{value}'");
            }

            width = val;
        }

        var precision = new Int32?();
        var precisionFromArguments = false;
        if (state.Precision is ['*']) {
            precisionFromArguments = true;
        } else if (state.Precision.Count > 0) {
            var str = new String(state.Precision.ToArray());
            if (!Int32.TryParse(str, out var val)) {
                throw new NotSupportedException($"Unknown precision argument '{str}' in '{value}'");
            }

            precision = val;
        }

        var length = state.Length.Count > 0
            ? new String(state.Length.ToArray())
            : null;

        result = new PrintfFormatSpecifier(value, state.Type, state.Flags, width, widthFromArguments, precision, precisionFromArguments, length);
        return true;
    }

    private ref struct ParseState {
        public PrintfFlags Flags = PrintfFlags.None;
        public readonly List<Char> Width = new List<Char>();
        public readonly List<Char> Precision = new List<Char>();
        public readonly List<Char> Length = new List<Char>();
        public Char Type;

        public ParseState() {
        }
    }
}

/// <summary>
///   The flags of a printf format specifier.
/// </summary>
/// <remarks>
///   See https://cplusplus.com/reference/cstdio/printf/
/// </remarks>
[Flags]
public enum PrintfFlags {
    None = 0,

    /// <summary>
    ///   Left-justify within the given field width; Right justification is the
    ///   default (see width sub-specifier).
    /// </summary>
    LeftJustify = 1,

    /// <summary>
    ///   Forces to preceed the result with a plus or minus sign (+ or -) even
    ///   for positive numbers. By default, only negative numbers are preceded
    ///   with a - sign.
    /// </summary>
    ForceSign = 2,

    /// <summary>
    ///   If no sign is going to be written, a blank space is inserted before
    ///   the value.
    /// </summary>
    SpaceIfMissingSign = 4,

    /// <summary>
    ///   Used with o, x or X specifiers the value is preceeded with 0, 0x or 0X
    ///   respectively for values different than zero. Used with a, A, e, E, f,
    ///   F, g or G it forces the written output to contain a decimal point even
    ///   if no more digits follow. By default, if no digits follow, no decimal
    ///   point is written.
    /// </summary>
    Alternate = 8,

    /// <summary>
    ///   Left-pads the number with zeroes (0) instead of spaces when padding is
    ///   specified (see width sub-specifier).
    /// </summary>
    ZeroPad = 16,
}