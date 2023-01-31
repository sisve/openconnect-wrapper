This repository contains a C# console application that uses the OpenConnect library to establish a vpn connection to a specified url. Credentials are provided using standard operating system apis, with the option to persist the credentials.

# Supported functionality and platforms

* 2FA with Duo MFA/Mobile (`--secondary-password push`)
* 2FA with SMS

## Functionality

|                     | Windows 10 | macOS 10.15 |
|---------------------|------------|-------------|
| Persist credentials | Yes        | (WIP)       |
| Login with webview  | Yes        | (WIP)       |

## Tested platforms 

* Windows 10 64bit
* macOS 10.15 Catalina 64bit Intel

# Windows

## Getting started on Windows

### Installing OpenConnect on Windows

1. Download OpenConnect from https://gitlab.com/openconnect/openconnect/-/jobs/artifacts/master/download?job=MinGW64/GnuTLS
   The link only works sporadically, for unknown reasons. If it doesn't work, follow these steps to find the download:
   1. Start at https://gitlab.com/openconnect/openconnect/-/pipelines?scope=finished&page=1&ref=master
   2. From above, try the "Download artifacts" button until you find the pipeline that has downloads.
   3. Download the artifact named `MinGW64/GnuTLS:archive`
2. Extract openconnect-installer.exe from the downloaded file.
3. Right-click openconnect-installer.exe, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
4. Run openconnect-installer.exe
   1. When asked, accept to install TAP-Windows.
   2. When TAP-Windows asks, choose to install TAP Utilities.

### Downloading openconnect-wrapper on Windows

1. Go to the latest release at https://github.com/sisve/openconnect-wrapper/releases/latest
   1. If you need webview support, or if you are unsure, download `connect-to-url.win-x64.webview.exe`
   2. If you do not need webview support, download `connect-to-url.win-x64.exe`
   3. Right-click the downloaded file, and click Properties. If the notice "This file came from another computer and might be blocked to help protect your computer" is visible, check the checkbox "Unlock". Click OK to save any changes and close the properties window.
2. Create a shortcut on your desktop to `\path\to\connect-to-url.win-x64.webview.exe https://vpn.domain.com/group`
3. Configure the shortcut to run as administrator.

To connect to several vpns, read more about multiple connections below, and repeat step 6 and 7 above to create a shortcut for every vpn.

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

Every vpn connection uses a "TAP virtual ethernet adapter". TAP-Windows created one during installation, but you need to create more if you want to connect to several vpns concurrently. To add another ethernet adapter, find the "Add a new TAP virtual ethernet adapter" on your start menu, and execute it with administrator privileges.

The start menu entry, and the bat file mention below, is part of the TAP Utilities that was installed during the TAP-Windows installation.

* You can right-click the start menu entry, wait for the folder `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\TAP-Windows\Utilities` to open, then rightclick the shortcut and click Run as administrator. You're done.
* If the start menu entry is missing, open a command prompt as Administrator and execute `"C:\Program Files\TAP-Windows\bin\tapinstall.exe" install "C:\Program Files\TAP-Windows\driver\OemVista.inf" tap0901`

To later remove all virtual ethernet adapters, use the above steps for the start menu entry "Delete ALL TAP virtual ethernet adapters", or execute `"C:\Program Files\TAP-Windows\bin\tapinstall.exe" remove tap0901` in a command prompt running with administrator privileges.

# Development and compiling

## Windows 10

1. Install OpenConnect according to the Getting Started guide
2. Install .NET 7 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/7.0
3. Checkout
4. Compile

## Mac

1. Install OpenConnect according to the Getting Started guide
2. Install .NET 7 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/7.0
3. Checkout
4. Compile