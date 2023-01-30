using System;
using System.IO;

namespace ConnectToUrl;

internal static class Program {
    private const Int32 SUCCESS = 0;
    private const Int32 FAILURE = 1;

    public static Int32 Main(String[] args) {
        CommandLineArgs? parsedArgs;
        if (!CommandLineArgs.TryParse(args, out parsedArgs)) {
            Console.Error.WriteLine("Failed to parse command line arguments.");
            return FailWithExitCode(FAILURE);
        }

        if (args.Length == 0) {
            Console.Error.WriteLine("Expected a single parameter with the url to connect to.");
            return FailWithExitCode(FAILURE);
        }

        var dllDirectory = @"C:\Program Files\OpenConnect";
        var dllPath = Path.Combine(dllDirectory, "libopenconnect-5.dll");
        if (!File.Exists(dllPath)) {
            Console.Error.WriteLine($"Missing file {dllPath}, have you installed OpenConnect?");
            return FailWithExitCode(FAILURE);
        }

        Console.WriteLine($"IntPtr.Size={IntPtr.Size}");

        using (ConsoleQuickEdit.Disable())
        using (var vpncScript = VpnScript.Scoped()) {
            // Make [DllImport] load libopenconnect from dllDirectory.
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);

            Console.WriteLine($"Using vpnc script at {vpncScript.ScriptPath}");
            var connection = new Connection {
                Url = parsedArgs.Url,
                MinLoggingLevel = (Int32)parsedArgs.LogLevel,
                ScriptPath = vpncScript.ScriptPath,
                SecondaryPassword = parsedArgs.SecondaryPassword,
            };

            var connectResult = connection.Connect();
            if (connectResult != SUCCESS) {
                return FailWithExitCode(connectResult);
            }

            Console.CancelKeyPress += (_, eventArgs) => {
                Console.WriteLine("Console.CancelKeyPress triggered");
                eventArgs.Cancel = true;
                connection.Disconnect();
            };

            connection.WaitForDisconnect();

            return SUCCESS;
        }
    }

    private static Int32 FailWithExitCode(Int32 exitCode) {
        if (Environment.GetEnvironmentVariable("PROMPT") == null) {
            // The PROMPT environment variable is present when executed from a
            // command prompt, but is missing when executed from a shortcut.
            //
            // We want the window to stay open in case of failures, so the user
            // can read the output to debug the problem.
            Console.WriteLine();
            Console.WriteLine("<Press enter to exit>");
            Console.ReadLine();
        }

        return exitCode;
    }
}