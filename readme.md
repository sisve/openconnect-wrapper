This repository contains a C# console application that uses the OpenConnect library to establish a vpn connection to a specified url. Credentials are provided using standard operating system apis, with the option to persist the credentials.

# Supported functionality and platforms

* 2FA with Duo MFA/Mobile (`--secondary-password push`)
* 2FA with SMS

## Functionality

|                     | Windows 10 | macOS 10.15 | Ubuntu 24.04          |
|---------------------|------------|-------------|-----------------------|
| Persist credentials | Yes        | Not yet     | Yes, using libsecret* |
| Login with webview  | Yes        | Not yet     | Yes, using libsecret* |

Libsecret stores credentials in a local keyring. This will be the root's keyring since we run the application using sudo. 

## Tested platforms 

* Windows 10 64bit
* macOS 10.15 Catalina 64bit Intel
* Ubuntu 24.04 Desktop

# Windows

## Getting started on Windows

### Installing TAP-Windows

TAP-Windows as originally packaged with OpenConnect, but has since been split 
into a separate package that needs to be installed separately.

1. Download the latest version of TAP-Windows from https://build.openvpn.net/downloads/releases/
2. Right-click the downloaded file, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
3. Run the downloaded file

### Installing OpenConnect on Windows

#### The easy way

1. Download [openconnect-installer-MinGW64-GnuTLS-9.12.git.36.07a4dd2-0.fc40.exe](https://github.com/sisve/openconnect-wrapper/raw/master/deps/openconnect-installer-MinGW64-GnuTLS-9.12.git.36.07a4dd2-0.fc40.exe) from this repository.
2. Right-click the downloaded installer, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
3. Run the installer.

#### The harder way

1. Download the installer from the internet
    1. Go to https://copr.fedorainfracloud.org/coprs/dwmw2/openconnect/package/mingw-openconnect/ which hosts the builds for openconnect
    2. Pick the newest successful build
    3. Under Results, pick the chroot name `fedora-rawhide-x86_64`
    4. Download the `mingw64-openconnect-installer-...` _rpm_ package.
    5. Extract the rpm using something that supports zstd. (7-Zip 23.01, dated 2023-06-20, does not.)
2. Right-click the downloaded installer, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
3. Run the installer.

### Downloading openconnect-wrapper on Windows

1. Go to the latest release at https://github.com/sisve/openconnect-wrapper/releases/latest
   1. If you need webview support, or if you are unsure, download `connect-to-url.win-x64.webview.exe`
   2. If you do not need webview support, download `connect-to-url.win-x64.exe`
   3. Right-click the downloaded file, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
2. Create a shortcut on your desktop to `\path\to\connect-to-url.win-x64.webview.exe https://vpn.domain.com/group`
3. Configure the shortcut to run as administrator.

To connect to several vpns, read more about multiple connections below, and repeat step 2 and 3 above to create a shortcut for every vpn.

## Persisting credentials on Windows

This application can persist your vpn credentials between logins. Just check the checkbox to save the credentials, and Windows will handle it internally. The credentials are stored in Windows Credential Manager. To remove any persisted credentials, remove them from Windows Credential Manager.

# Mac

## Getting started on Mac

This assumes that you have homebrew installed. Installation instructions for Homebrew can be found at https://brew.sh/

1. Install OpenConnect (if you're using homebrew; `homebrew install openconnect`)
2. Go to the latest release at https://github.com/sisve/openconnect-wrapper/releases/latest
   1. Download `connect-to-url.osx-x64`
3. Using Script Editor, create a new script
   ```
   do shell script "\"/path/to/connect-to-url.osx-x64\" https://vpn-domain.com/group"
   quit
   ```
4. Save the script on the desktop

# Ubuntu 24.04 Desktop

## Getting started on Ubuntu

1. Install OpenConnect (`apt install openconnect`) or the openconnect libraries (`apt install libopenconnect5`)
2. Go to the latest release at https://github.com/sisve/openconnect-wrapper/releases/latest
   1. Download `connect-to-url.linux-x64`
   2. Mark the file as executable `chmod a+x connect-to-url.linux-x64`
3. Create a script
   ```
   #!/usr/bin/env bash
   sudo /path/to/connect-to-url.linux-x64 https://vpn-domain.com/group
   ```
4. Save the script on the desktop

If you get `Failed to execute child process "dbus-launch" (Nu such file or directory)`, install dbus-x11 using `sudo apt install dbus-x11`

To avoid being asked for your password at every launch (for sudo), add the following using `sudo visudo -f /etc/sudoers.d/vpn`.

```
your_username ALL = (root) NOPASSWD: /path/to/connect-to-url.linux-x64 *
```

# Commandline options

* `--secondary-password push` will enter "push" as a secondary password. This is meant to automate the connection process when using Duo MFA.
* `--log-level (error|warning|info|debug|trace)` configures the logging level. This is intended for debugging purposes.

# Multiple connections

This applications supports multiple concurrent vpn connections, with some requirements.
 
* Only one vpn can have a default gateway (sends all traffic over the vpn). 
* All vpn networks should have unique addresses. We cannot handle cases where an ip address is available in two different places.
* You need to add more virtual ethernet adapters, one for every vpn. 

If the option exists, prefer connect to vpns that are running as "split tunneling". This means that they declare some routes that should go over the vpn connection, and let the rest of the traffic stay on your local network.

## Add more virtual ethernet adapters on Windows

Every vpn connection uses a "TAP virtual ethernet adapter". TAP-Windows created one during installation, but you need to create more if you want to connect to several vpns concurrently. To add another ethernet adapter, open a command prompt as Administrator and execute `"C:\Program Files\TAP-Windows\bin\tapinstall.exe" install "C:\Program Files\TAP-Windows\driver\OemVista.inf" tap0901`

To later remove all virtual ethernet adapters, execute `"C:\Program Files\TAP-Windows\bin\tapinstall.exe" remove tap0901` in a command prompt running with administrator privileges.

# Development and compiling

Note that administrator/root privileges are required for adding new network interfaces. You may need to run your development tools as administrator/root to be able to run/debug the application.

## Windows 10

1. Install OpenConnect according to the Getting Started guide
2. Install .NET 8 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. Checkout
4. Compile

## Mac

1. Install OpenConnect according to the Getting Started guide
2. Install .NET 8 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. Checkout
4. Compile

## Ubuntu 24.04 Desktop

1. Install OpenConnect according to the Getting Started guide
2. Install .NET 8 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
3. Checkout
4. Compile


# Third-Party Software Licenses

This project dynamically loads and invokes [OpenConnect](https://gitlab.com/openconnect/openconnect) (LGPLv2.1) using standard `[DllImport(...)]` declarations.

This project also contains _modified_ vpnc-scripts, taken from [openconnect/vpnc-scripts](https://gitlab.com/openconnect/vpnc-scripts) (GPL v2.0 or later). These are embedded into the compiled executable, extracted at runtime and invoked. This classifies as redistribution, and as such the project is also licensed as GPL v2.0 or later. 
