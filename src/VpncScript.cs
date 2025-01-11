﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ConnectToUrl;

/// <summary>
///   A helper class that writes the vpnc script to disk, and does its best to
///   remove it once the object goes out of scope (read: disposed). This class
///   will also try and cleanup previous executions that may have crashed and
///   left an orphan vpnc script file.
/// </summary>
internal class VpnScript : DisposableAction {
    public String ScriptPath { get; }

    public VpnScript(String scriptPath, Action action) : base(action) {
        ScriptPath = scriptPath;
    }

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("OSX")]
    [SupportedOSPlatform("Linux")]
    public static VpnScript Scoped() {
        String filenameBase;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            filenameBase = "vpnc-script-win";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            filenameBase = "vpnc-script";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            filenameBase = "vpnc-script";
        } else {
            throw new PlatformNotSupportedException();
        }

        CleanupUnusedFiles(filenameBase);

        var file = CreateFiles(filenameBase);

        return new VpnScript(file.ScriptPath, () => {
            file.LockStream.Close();
            file.LockStream.Dispose();

            try {
                File.Delete(file.ScriptPath);
            } catch (IOException) {
            }
        });
    }

    private static void CleanupUnusedFiles(String filenameBase) {
        var existingFiles = Directory.EnumerateFiles(AppContext.BaseDirectory, $"{filenameBase}.*.js");
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

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("OSX")]
    [SupportedOSPlatform("Linux")]
    private static Files CreateFiles(String filenameBase) {
        for (var attempt = 0; attempt < 10; ++attempt) {
            var random = Path.GetRandomFileName();
            var scriptPath = Path.Combine(AppContext.BaseDirectory, $"{filenameBase}.{random}.js");
            var lockPath = Path.Combine(AppContext.BaseDirectory, $"{filenameBase}.{random}.js.lock");
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

#if MACOS || LINUX
            var statResult = Mono.Unix.Native.Syscall.stat(scriptPath, out var statBuf);
            if (statResult != 0) {
                var errno = Mono.Unix.Native.Stdlib.GetLastError();
                var errmsg = Mono.Unix.Native.Stdlib.strerror(errno);
                Console.WriteLine($"stat returned error {statResult}, errno={errno}, errmsg='{errmsg}', retrying.");
                continue;
            }

            var perms = statBuf.st_mode;
            var newPerms = perms | Mono.Unix.Native.FilePermissions.S_IXUSR;
            var chmodResult = Mono.Unix.Native.Syscall.chmod(scriptPath, newPerms);
            if (chmodResult != 0) {
                var errno = Mono.Unix.Native.Stdlib.GetLastError();
                var errmsg = Mono.Unix.Native.Stdlib.strerror(errno);
                Console.WriteLine($"chmod returned error {chmodResult}, errno={errno}, errmsg='{errmsg}', retrying.");
                continue;
            }
#endif

            return new Files(scriptPath, lockStream);
        }

        throw new IOException("Failed to initialize the vpnc script after several attempts.");
    }

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("OSX")]
    [SupportedOSPlatform("Linux")]
    private static String GetVpncScriptContent() {
        var assembly = typeof(Program).Assembly;
        String resourceName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            resourceName = $"{assembly.GetName().Name}.vpnc-script-win.js";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            resourceName = $"{assembly.GetName().Name}.vpnc-script-osx.sh";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            resourceName = $"{assembly.GetName().Name}.vpnc-script-linux.sh";
        } else {
            throw new PlatformNotSupportedException();
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var streamReader = new StreamReader(stream!);
        return streamReader.ReadToEnd();
    }
}