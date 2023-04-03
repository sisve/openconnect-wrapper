using System;
using System.Reflection;
using System.Runtime.InteropServices;

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

        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, ResolveLibrary);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            /* allow */
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            /* allow */
        } else {
            Console.WriteLine("This application does not support your operating system.");
            Console.WriteLine($"RuntimeInformation.RuntimeIdentifier='{RuntimeInformation.RuntimeIdentifier}'");
            return FailWithExitCode(FAILURE);
        }

        if (!Platform.OSFunctionality.HasPermissions()) {
            return FailWithExitCode(FAILURE);
        }

        if (!Platform.OSFunctionality.CheckForOpenConnectInstallation()) {
            return FailWithExitCode(FAILURE);
        }

        Console.WriteLine($"IntPtr.Size={IntPtr.Size}");
        Console.WriteLine($"RuntimeInformation.RuntimeIdentifier='{RuntimeInformation.RuntimeIdentifier}'");

        using (ConsoleQuickEdit.Disable())
        using (var vpncScript = VpnScript.Scoped()) {
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

    private static IntPtr ResolveLibrary(String libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        if (libraryName == OpenConnect.DllName && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (NativeLibrary.TryLoad(OpenConnect.WindowsDllName, assembly, searchPath, out var handle)) {
                return handle;
            }
        }

        {
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle)) {
                return handle;
            }
        }

        Console.Error.WriteLine($"Resolver: Failed to resolve library '{libraryName}'");
        return IntPtr.Zero;
    }

    private static Int32 FailWithExitCode(Int32 exitCode) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.GetEnvironmentVariable("PROMPT") == null) {
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