using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Windows.Sdk;

namespace AltTilde
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
                    SwitchWindows((int)m.WParam == 1);
                    // @TODO: Should I still `base.WndProc(ref m)` here?
                }
            }

            private void SwitchWindows(bool forward)
            {
                List<HWND> topLevelWindows = new List<HWND>();
                HWND foregroundHandle = PInvoke.GetForegroundWindow();
                if (foregroundHandle.Value > 0)
                {
                    uint threadId = 0;
                    uint processId;
                    unsafe
                    {
                        uint tmpProcessId = 0;
                        uint* processIdPtr = (uint*)&tmpProcessId;
                        threadId = PInvoke.GetWindowThreadProcessId(foregroundHandle, processIdPtr);
                        processId = tmpProcessId;
                    }

                    PInvoke.EnumThreadWindows(threadId, (HWND hwnd, LPARAM customParam) =>
                    {
                        if (PInvoke.IsWindowVisible(hwnd)) {
                            if (PInvoke.GetParent(hwnd).Value == 0)
                            {
                                // @TODO: Is more filtering needed here?
                                topLevelWindows.Add(hwnd);
                            }
                        }
                        return true;
                    }, (LPARAM)0);

                    if (topLevelWindows.Count == 1)
                    {
                        // Try another way; look for all windows with common process IDs
                        PInvoke.EnumWindows((HWND hwnd, LPARAM customParam) =>
                        {
                            if (PInvoke.IsWindowVisible(hwnd)) {
                                if (PInvoke.GetParent(hwnd).Value == 0)
                                {
                                    uint windowThreadId = 0;
                                    uint windowProcessId = 0;
                                    unsafe
                                    {
                                        uint* windowProcessIdPtr = (uint*)&windowProcessId;
                                        windowThreadId = PInvoke.GetWindowThreadProcessId(hwnd, windowProcessIdPtr);
                                    }
                                    if (windowThreadId != threadId && windowProcessId == processId)
                                    {
                                        // @TODO: Is more filtering needed here?
                                        topLevelWindows.Add(hwnd);
                                    }
                                }
                            }
                            return true;
                        }, (LPARAM)0);

                    }

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
                            ActivateWindow(topLevelWindows[newWindowIndex]);
                        }
                    }
                }
            }

            private void ActivateWindow(HWND handle)
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

                InitHandlers();
            }

            private void InitHandlers()
            {
                // @TODO: Check for FALSE responses.
                PInvoke.RegisterHotKey((HWND)messageForm.Handle, 1, RegisterHotKey_fsModifiersFlags.MOD_ALT | RegisterHotKey_fsModifiersFlags.MOD_NOREPEAT, VK_OEM_3);
                PInvoke.RegisterHotKey((HWND)messageForm.Handle, 2, RegisterHotKey_fsModifiersFlags.MOD_ALT | RegisterHotKey_fsModifiersFlags.MOD_NOREPEAT | RegisterHotKey_fsModifiersFlags.MOD_SHIFT, VK_OEM_3);
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
