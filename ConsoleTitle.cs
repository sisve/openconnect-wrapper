using System;
using System.Diagnostics.CodeAnalysis;

namespace ConnectToUrl;

internal static class ConsoleTitle {
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static IDisposable Change(String newTitle) {
        try {
            var oldTitle = Console.Title;
            Console.Title = newTitle;

            return new DisposableAction(() => {
                Console.Title = oldTitle;
            });
        } catch (PlatformNotSupportedException) {
            return DisposableAction.Noop;
        }
    }
}