# AltTilde

## Overview

Modern Windows development is easier than ever for me, a former Mac-head, especially with WSL2 and its ability to run both Windows and Linux commands.

The one keyboard shortcut I truly miss on Windows machines is Command/Alt-`, which cycles through windows in the foreground application. Both OS X and GNOME support this.

## Technologies and Acknowledgements

This application is a .NET 5.0 app that uses https://github.com/microsoft/CsWin32 to implement calls to the Win32 API in order to enumerate the list of windows and switch between them.

I am a newcomer to Windows GUI programming, so http://www.thescarms.com/vbasic/alttab.aspx was immensly helpful in understanding what Win32 APIs would be needed to accomplish this task.

## License and Support

This app is licensed under the MIT open source license, and I will try to fix issues if they are reported, but this comes with no warranty, especially since this is my first Win32 app.
