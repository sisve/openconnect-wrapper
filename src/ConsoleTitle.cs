using System;
using System.Runtime.InteropServices;

namespace ConnectToUrl;

internal static class ConsoleTitle {
    public static IDisposable Change(String newTitle) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var oldTitle = Console.Title;
            Console.Title = newTitle;

            return new DisposableAction(() => {
                Console.Title = oldTitle;
            });
        }

        return DisposableAction.Noop;
    }
}