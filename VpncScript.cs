using System;
using System.IO;

namespace ConnectToUrl;

internal class VpnScript : DisposableAction {
    public String ScriptPath { get; }

    public VpnScript(String scriptPath, Action action) : base(action) {
        ScriptPath = scriptPath;
    }

    public static VpnScript Scoped() {
        CleanupUnusedFiles();

        var file = CreateFiles();

        return new VpnScript(file.ScriptPath, () => {
            file.LockStream.Close();
            file.LockStream.Dispose();

            try {
                File.Delete(file.ScriptPath);
            } catch (IOException) {
            }
        });
    }

    private static void CleanupUnusedFiles() {
        var existingFiles = Directory.EnumerateFiles(AppContext.BaseDirectory, "vpnc-script-win.*.js");
        foreach (var existingFile in existingFiles) {
            var lockFile = existingFile + ".lock";
            try {
                if (File.Exists(lockFile)) {
                    File.Delete(lockFile);
                }

                File.Delete(existingFile);
            } catch (IOException) {
            }
        }
    }

    private record Files(String ScriptPath, FileStream LockStream);

    private static Files CreateFiles() {
        for (var attempt = 0; attempt < 10; ++attempt) {
            var random = Path.GetRandomFileName();
            var scriptPath = Path.Combine(AppContext.BaseDirectory, $"vpnc-script-win.{random}.js");
            var lockPath = Path.Combine(AppContext.BaseDirectory, $"vpnc-script-win.{random}.js.lock");
            if (File.Exists(scriptPath) || File.Exists(lockPath)) {
                continue;
            }

            var filestreamOptions = new FileStreamOptions {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.DeleteOnClose,
            };

            FileStream lockStream;

            try {
                lockStream = new FileStream(lockPath, filestreamOptions);
                File.WriteAllText(scriptPath, GetVpncScriptContent());
            } catch (IOException ex) {
                Console.WriteLine($"IOException: '{ex.Message}' when initalizing vpnc script, retrying.");
                continue;
            }

            return new Files(scriptPath, lockStream);
        }

        throw new IOException("Failed to initialize the vpnc script after several attempts.");
    }

    private static String GetVpncScriptContent() {
        var assembly = typeof(Program).Assembly;

        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.vpnc-script-win.js");
        using var streamReader = new StreamReader(stream!);
        return streamReader.ReadToEnd();
    }
}