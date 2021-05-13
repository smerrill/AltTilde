using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;

namespace AltTilde2
{
    static class Program
    {
        // A hidden form that will receive our WM_HOTKEY messages.
        // This feels like a hack, but I'm new to Windows programming.
        public class MessageForm : System.Windows.Forms.Form
        {
            public MessageForm()
            {
                this.Visible = false;
            }

            protected override void WndProc(ref Message m)
            {
                // WM_HOTKEY
                if (m.Msg != 0x312)
                {
                    base.WndProc(ref m);
                }
                else
                {
                    switchWindows((int)m.WParam == 1);
                }
            }

            private void switchWindows(bool forward)
            {
                List<HWND> topLevelWindows = new List<HWND>();
                HWND foregroundHandle = PInvoke.GetForegroundWindow();
                if (foregroundHandle.Value > 0)
                {
                    uint threadId = 0;
                    unsafe
                    {
                        threadId = PInvoke.GetWindowThreadProcessId(foregroundHandle);
                    }

                    PInvoke.EnumThreadWindows(threadId, (HWND hwnd, LPARAM customParam) =>
                    {
                        if (PInvoke.IsWindowVisible(hwnd)) {
                            if (PInvoke.GetParent(hwnd).Value == 0)
                            {
                                topLevelWindows.Add(hwnd);

                                //var hasOwner = (PInvoke.GetWindow(hwnd, GetWindow_uCmdFlags.GW_OWNER).Value != 0);
                                //if (!hasOwner)
                                //{
                                //    topLevelWindows.Add(hwnd);
                                //}
                                /*
                                var extendedStyles = PInvoke.GetWindowLong(hwnd, GetWindowLongPtr_nIndex.GWL_EXSTYLE);

                                // WS_EX_APPWINDOW
                                if ((extendedStyles & 0x00040000L) == 0x00040000L) {
                                    topLevelWindows.Add(hwnd);
                                }
                                */
                            }
                        }
                        return true;
                    }, (LPARAM)0);

                    if (topLevelWindows.Count > 1)
                    {
                        topLevelWindows.Sort((HWND x, HWND y) =>
                        {
                            if (x.Value > y.Value)
                            {
                                return 1;
                            }
                            else
                            {
                                return -1;
                            }
                        });

                        var currentWindowIndex = topLevelWindows.IndexOf(foregroundHandle);
                        var newWindowIndex = -1;
                        if (currentWindowIndex != -1)
                        {
                            if (forward) {
                                if (currentWindowIndex == topLevelWindows.Count - 1)
                                {
                                    newWindowIndex = 0;
                                }
                                else
                                {
                                    newWindowIndex = currentWindowIndex + 1;
                                }
                            } else
                            {
                                if (currentWindowIndex == 0)
                                {
                                    newWindowIndex = topLevelWindows.Count - 1;
                                } else
                                {
                                    newWindowIndex = currentWindowIndex - 1;
                                }
                            }
                        }

                        if (newWindowIndex != -1)
                        {
                            activateWindow(topLevelWindows[newWindowIndex]);
                        }
                    }

                    //uint processId = 0, threadId;

                    //unsafe
                    //{
                    //    uint* pid = (uint*)0;
                    //    threadId = PInvoke.GetWindowThreadProcessId(handle, null);
                    //    if (pid != null)
                    //    {
                    //        processId = (uint)pid;
                    //    }
                    //}

                    //Console.WriteLine(threadId);
                    //Console.WriteLine(processId);
                }
            }

            private void activateWindow(HWND handle)
            {
                PInvoke.SetForegroundWindow(handle);
                // @TODO: Do I need to AttachThreadInput?
                if (PInvoke.IsIconic(handle))
                {
                    PInvoke.ShowWindow(handle, SHOW_WINDOW_CMD.SW_RESTORE);
                }
                else
                {
                    PInvoke.ShowWindow(handle, SHOW_WINDOW_CMD.SW_SHOW);
                }
            }
        }

        public class AltTildeContext : ApplicationContext
        {
            private readonly NotifyIcon trayIcon;
            private readonly Form messageForm;
            const uint VK_OEM_3 = 0xC0;

            public AltTildeContext()
            {
                messageForm = new MessageForm();

                // Initialize Tray Icon
                trayIcon = new NotifyIcon()
                {
                    Icon = Properties.Resources.AppIcon,
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true,
                };
                var exitItem = new ToolStripMenuItem("Exit", null, Exit);
                trayIcon.ContextMenuStrip.Items.Add(exitItem);

                initHandlers();
            }

            private void initHandlers()
            {
                // @TODO: Check for FALSE responses.
                PInvoke.RegisterHotKey((HWND)messageForm.Handle, 1, RegisterHotKey_fsModifiersFlags.MOD_ALT | RegisterHotKey_fsModifiersFlags.MOD_NOREPEAT, VK_OEM_3);
                PInvoke.RegisterHotKey((HWND)messageForm.Handle, 2, RegisterHotKey_fsModifiersFlags.MOD_ALT | RegisterHotKey_fsModifiersFlags.MOD_NOREPEAT | RegisterHotKey_fsModifiersFlags.MOD_SHIFT, VK_OEM_3);
            }

            string GetForegroundApp()
            {
                HWND handle = PInvoke.GetForegroundWindow();
                if (handle.Value > 0)
                {
                    uint threadId = 0;
                    unsafe
                    {
                        threadId = PInvoke.GetWindowThreadProcessId(handle);
                    }

                    PInvoke.EnumThreadWindows(threadId, (HWND hwnd, LPARAM customParam) =>
                    {

                        return true;
                    }, (LPARAM)0);
                    Console.WriteLine(threadId);

                    //uint processId = 0, threadId;

                    //unsafe
                    //{
                    //    uint* pid = (uint*)0;
                    //    threadId = PInvoke.GetWindowThreadProcessId(handle, null);
                    //    if (pid != null)
                    //    {
                    //        processId = (uint)pid;
                    //    }
                    //}

                    //Console.WriteLine(threadId);
                    //Console.WriteLine(processId);
                }

                return "Unknown";
            }

            static void GetAllWindowsInfo()
            {
                bool windowReturn = PInvoke.EnumWindows(
                    (HWND handle, LPARAM customParam) =>
                    {
                        int bufferSize = PInvoke.GetWindowTextLength(handle) + 1;
                        unsafe
                        {
                            fixed (char* windowNameChars = new char[bufferSize])
                            {
                                if (PInvoke.GetWindowText(handle, windowNameChars, bufferSize) == 0)
                                {
                                    int errorCode = Marshal.GetLastWin32Error();
                                    if (errorCode != 0)
                                    {
                                        throw new Win32Exception(errorCode);
                                    }

                                    return true;
                                }

                                string windowName = new string(windowNameChars);
                                Console.WriteLine(windowName);
                            }

                            return true;
                        }
                    },
                    (LPARAM)0);
            }

            void Exit(object sender, EventArgs e)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;

                // @TODO: Check for FALSE responses.
                PInvoke.UnregisterHotKey((HWND)messageForm.Handle, 1);
                PInvoke.UnregisterHotKey((HWND)messageForm.Handle, 2);
                messageForm.Close();

                Application.Exit();
            }
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Application.Run(new Form1());
            Application.Run(new AltTildeContext());
        }
    }
}
