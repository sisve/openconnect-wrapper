using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandLine;

namespace ConnectToUrl; 

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
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
                    "-Verbose" => new[] { "--secret-verbose" },
                    "-DumpHttpTraffic" => new[] { "--secret-dump-http-traffic" },
                    _ => new[] { arg },
                };
            })
            .SelectMany(arg => arg)
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
            result.LogLevel = LogLevelEnum.Debug;
        }

        if (result is { DumpHttpTraffic: true, LogLevel: < LogLevelEnum.Trace }) {
            result.LogLevel = LogLevelEnum.Trace;
        }
        
        return true;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal enum LogLevelEnum {
        Error = OpenConnect.PRG_ERR,
        // "Warning" is an expected log level, but does not exist within OpenConnect.
        Warning = OpenConnect.PRG_ERR,
        Info = OpenConnect.PRG_INFO,
        Debug = OpenConnect.PRG_DEBUG,
        Trace = OpenConnect.PRG_TRACE,
    }
}