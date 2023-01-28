using System;
using System.Runtime.InteropServices;
using ConnectToUrl;

internal static class ConsoleQuickEdit {
    // This flag enables the user to use the mouse to select and edit text.
    // To enable this mode, use ENABLE_QUICK_EDIT_MODE | ENABLE_EXTENDED_FLAGS.
    // To disable this mode, use ENABLE_EXTENDED_FLAGS without this flag.
    // Source: https://docs.microsoft.com/en-us/windows/console/setconsolemode#parameters
    private const UInt32 ENABLE_QUICK_EDIT = 0x0040;

    // The standard input device. Initially, this is the console input buffer, CONIN$.
    // Source: https://docs.microsoft.com/en-us/windows/console/getstdhandle#parameters
    private const Int32 STD_INPUT_HANDLE = -10;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(Int32 nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern Boolean GetConsoleMode(IntPtr hConsoleHandle, out UInt32 lpMode);

    [DllImport("kernel32.dll")]
    private static extern Boolean SetConsoleMode(IntPtr hConsoleHandle, UInt32 dwMode);

    public static IDisposable Disable() {
        IntPtr stdinHandle;

        try {
            stdinHandle = GetStdHandle(STD_INPUT_HANDLE);
        } catch (DllNotFoundException) {
            Console.Error.WriteLine("GetConsoleMode failed, console quick-edit has been left as-is.");
            return DisposableAction.Noop;
        }

        UInt32 consoleMode;
        if (!GetConsoleMode(stdinHandle, out consoleMode)) {
            Console.Error.WriteLine("GetConsoleMode failed, console quick-edit has been left as-is.");
            return DisposableAction.Noop;
        }

        if ((consoleMode & ENABLE_QUICK_EDIT) == 0) {
            Console.WriteLine("Console quick-edit already disabled.");
            return DisposableAction.Noop;
        }

        // Clear the ENABLE_QUICK_EDIT flag.
        consoleMode &= ~ENABLE_QUICK_EDIT;

        if (!SetConsoleMode(stdinHandle, consoleMode)) {
            Console.Error.WriteLine("SetConsoleMode failed, console quick-edit has been left as-is.");
            return DisposableAction.Noop;
        }

        Console.WriteLine("Console quick-edit disabled.");

        return new DisposableAction(() => {
            consoleMode = consoleMode |= ENABLE_QUICK_EDIT;

            if (!SetConsoleMode(stdinHandle, consoleMode)) {
                Console.Error.WriteLine("SetConsoleMode failed, console quick-edit was not restored.");
            }
        });
    }
}