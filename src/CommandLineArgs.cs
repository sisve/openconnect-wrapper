using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandLine;

namespace ConnectToUrl;

/// <summary>
///   Contains all parsing of command line arguments into a structured format.
/// </summary>
/// <remarks>
///   CommandLineParser uses reflection to set the properties of this class.
///   This implies that static code analysis cannot see the usage of the
///   property settings, and application trimming removes them. This is handled
///   by the TrimRoots.xml file, where we specify that we want to keep all
///   members of this class. 
/// </remarks>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal class CommandLineArgs {
    [Value(0, MetaName = "Url", Required = true, HelpText = "Url to vpn")]
    public String Url { get; set; } = String.Empty;

    [Option("secondary-password", HelpText = "Secondary password to auto-fill for the first connection attempt.")]
    public String? SecondaryPassword { get; set; }

    [Option("log-level", Default = LogLevelEnum.Info)]
    public LogLevelEnum LogLevel { get; set; }

    #region backward compatibility

    [Option("secret-verbose", Hidden = true)]
    public Boolean Verbose { get; set; }

    [Option("secret-dump-http-traffic", Hidden = true)]
    public Boolean DumpHttpTraffic { get; set; }

    #endregion

    internal static Boolean TryParse(String[] args, [NotNullWhen(true)] out CommandLineArgs? result) {
        // Make sure application trimming keep the constructor.
        GC.KeepAlive(new CommandLineArgs());

        // Preprocess args to support parameters that the old powershell script
        // supported.
        args = args.Select(arg => {
                return arg switch {
                    "-Verbose" => "--secret-verbose",
                    "-DumpHttpTraffic" => "--secret-dump-http-traffic",
                    _ => arg,
                };
            })
            .ToArray();

        var parser = new Parser(opt => {
            opt.HelpWriter = Console.Error;
            opt.CaseInsensitiveEnumValues = true;
        });

        var parseResult = parser.ParseArguments<CommandLineArgs>(args);
        if (parseResult!.Errors!.Any()) {
            result = null;
            return false;
        }

        result = parseResult.Value!;

        // Handle backward compatibility options
        if (result is { Verbose: true, LogLevel: < LogLevelEnum.Debug }) {
            Console.Error.WriteLine("CommandLineArgs: Called with -Verbose, change to '--log-level debug'");
            result.LogLevel = LogLevelEnum.Debug;
        }

        if (result is { DumpHttpTraffic: true, LogLevel: < LogLevelEnum.Trace }) {
            Console.Error.WriteLine("CommandLineArgs: Called with -DumpHttpTraffic, change to '--log-level trace'");
            result.LogLevel = LogLevelEnum.Trace;
        }

        return true;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal enum LogLevelEnum {
        Error = OpenConnect.PRG_ERR,

        // "Warning" is an expected log level, but does not exist within OpenConnect.
        [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
        Warning = OpenConnect.PRG_ERR,

        Info = OpenConnect.PRG_INFO,
        Debug = OpenConnect.PRG_DEBUG,
        Trace = OpenConnect.PRG_TRACE,
    }
}