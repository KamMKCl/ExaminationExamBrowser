using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace ExaminationExamBrowser
{
    public static class SecurityManager
    {
        #region Win32 API Imports

        // For keyboard hook
        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYDOWN = 0x0100;
        private static nint _hookID = nint.Zero;
        private static LowLevelKeyboardProc _keyboardProc;

        private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void DisableProcessWindowsGhosting();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, nint lpvParam, int fuWinIni);

        private const int SPI_SETSCREENSAVERRUNNING = 0x0061;
        private const int SPIF_SENDCHANGE = 0x0002;

        // Additional API for alternative Alt+Tab blocking approach
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint VK_TAB = 0x09; // Virtual key code for Tab

        #endregion Win32 API Imports

        #region Properties

        private static bool _securityApplied = false;

        #endregion Properties

        // Apply all security measures
        public static void ApplySecurity()
        {
            if (_securityApplied) return; // Don't apply twice

            try
            {
                // Registry-based restrictions
                DisableMultipleMonitors();
                DisableTaskManager();
                DisableShutdownAndLogoff();
                DisableSwitchUser();
                DisableLockWorkstation();
                DisableTaskManagerChangePassword();

                // Install keyboard hook to block system keys
                _keyboardProc = KeyboardHookCallback;
                _hookID = SetKeyboardHook(_keyboardProc);
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                // Disable Alt+Tab and other system key combinations
                DisableSystemKeys();

                DisableGameBarSystemWide();

                _securityApplied = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying security: {ex.Message}");
                throw;
            }
        }

        private static void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            // Re-check if another monitor was plugged in during the session
            if (Screen.AllScreens.Length > 1)
            {
                MessageBox.Show("External monitor detected during the exam. The session will now end.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableSecurity();
                Environment.Exit(1);
                Application.Exit();
                // Force close
            }
        }

        private static void DisableMultipleMonitors()
        {
            int screenCount = Screen.AllScreens.Length;

            if (screenCount > 1)
            {
                MessageBox.Show(
                    "Multiple monitors are detected. Please disconnect all external displays before starting the exam.",
                    "Security Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error

                );

                // Exit immediately to enforce security
                Environment.Exit(1);
            }
        }

        // Remove all security measures
        public static void DisableSecurity()
        {
            if (!_securityApplied) return; // Nothing to disable

            try
            {
                // Remove registry-based restrictions
                EnableTaskManager();
                EnableShutdownAndLogoff();
                EnableSwitchUser();
                EnableLockWorkstation();
                EnableTaskManagerChangePassword();

                // Remove keyboard hook
                if (_hookID != nint.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = nint.Zero;
                }

                // Re-enable system keys
                EnableSystemKeys();

                EnableGameBarSystemWide();

                _securityApplied = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing security: {ex.Message}");
                throw;
            }
        }

        #region Registry Modifications

        // Disable Task Manager for the current user
        private static void DisableTaskManager()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to disable Task Manager");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Task Manager: {ex.Message}");
                throw;
            }
        }

        // Disable Shutdown, Logoff and Restart for the current user
        private static void DisableShutdownAndLogoff()
        {
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("NoClose", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to disable Shutdown and Logoff");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Shutdown and Logoff: {ex.Message}");
                throw;
            }
        }

        // Disable Switch User for the current user
        private static void DisableSwitchUser()
        {
            try
            {
                // This setting needs to be in LocalMachine to properly hide Switch User
                using (RegistryKey regkey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                    }
                }

                // Also set it in CurrentUser for redundancy
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("NoSwitchUser", 1, RegistryValueKind.DWord);
                        regkey.SetValue("NoLogoff", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to disable Switch User");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Switch User: {ex.Message}");
                throw;
            }
        }

        public static void DisableTaskManagerChangePassword()
        {
            try
            {
                // Disable Change Password via Task Manager
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);
                    }
                }

                // Also set in LocalMachine for system-wide effect (requires admin privileges)
                using (RegistryKey regkey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null)
                    {
                        regkey.SetValue("DisableChangePassword", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to disable Task Manager password change");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Task Manager password change: {ex.Message}");
                throw;
            }
        }

        public static void EnableTaskManagerChangePassword()
        {
            try
            {
                // Enable Change Password via Task Manager in CurrentUser
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null && regkey.GetValue("DisableChangePassword") != null)
                    {
                        regkey.DeleteValue("DisableChangePassword", false);
                    }
                }

                // Also remove from LocalMachine if it exists
                using (RegistryKey regkey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null && regkey.GetValue("DisableChangePassword") != null)
                    {
                        regkey.DeleteValue("DisableChangePassword", false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to enable Task Manager password change");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Task Manager password change: {ex.Message}");
                throw;
            }
        }

        // Disable Lock Workstation (Windows+L)
        private static void DisableLockWorkstation()
        {
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null)
                    {
                        // Disable Lock Workstation
                        regkey.SetValue("DisableLockWorkstation", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to disable Lock Workstation");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Lock Workstation: {ex.Message}");
                throw;
            }
        }

        // Enable Task Manager for the current user
        private static void EnableTaskManager()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        if (key.GetValue("DisableTaskMgr") != null)
                        {
                            key.DeleteValue("DisableTaskMgr");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to enable Task Manager");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Task Manager: {ex.Message}");
                throw;
            }
        }

        // Enable Shutdown, Logoff and Restart for the current user
        private static void EnableShutdownAndLogoff()
        {
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (regkey != null && regkey.GetValue("NoClose") != null)
                    {
                        regkey.DeleteValue("NoClose", false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to enable Shutdown and Logoff");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Shutdown and Logoff: {ex.Message}");
                throw;
            }
        }

        // Enable Switch User for the current user
        private static void EnableSwitchUser()
        {
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null && regkey.GetValue("HideFastUserSwitching") != null)
                    {
                        regkey.DeleteValue("HideFastUserSwitching", false);
                    }
                }

                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (regkey != null)
                    {
                        if (regkey.GetValue("NoSwitchUser") != null)
                        {
                            regkey.DeleteValue("NoSwitchUser", false);
                        }

                        if (regkey.GetValue("NoLogoff") != null)
                        {
                            regkey.DeleteValue("NoLogoff", false);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to enable Switch User");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Switch User: {ex.Message}");
                throw;
            }
        }

        // Enable Lock Workstation (Windows+L)
        private static void EnableLockWorkstation()
        {
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (regkey != null && regkey.GetValue("DisableLockWorkstation") != null)
                    {
                        regkey.DeleteValue("DisableLockWorkstation", false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw new UnauthorizedAccessException("Insufficient privileges to enable Lock Workstation");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Lock Workstation: {ex.Message}");
                throw;
            }
        }

        #endregion Registry Modifications

        #region Keyboard Hook and System Keys

        private static nint SetKeyboardHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static nint KeyboardHookCallback(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Block Alt key itself to prevent Alt+Tab
                //if (key == Keys.Alt || key == Keys.LMenu || key == Keys.RMenu)
                //{
                //    return (IntPtr)1; // Block all Alt key presses
                //}

                // Block Tab key when Alt is down
                if (key == Keys.Tab && (Control.ModifierKeys & Keys.Alt) != 0)
                {
                    return 1; // Block Alt+Tab explicitly
                }

                // Block Windows key
                if (key == Keys.LWin || key == Keys.RWin)
                {
                    return 1; // Block this key
                }

                // Block Escape when Alt or Ctrl is pressed
                if (key == Keys.Escape &&
                    ((Control.ModifierKeys & Keys.Alt) != 0 || (Control.ModifierKeys & Keys.Control) != 0))
                {
                    return 1; // Block Alt+Esc and Ctrl+Esc
                }

                // Block Alt+F4
                if (key == Keys.F4 && (Control.ModifierKeys & Keys.Alt) != 0)
                {
                    return 1; // Block this key combination
                }

                // Block function keys
                if (key >= Keys.F1 && key <= Keys.F12)
                {
                    // Block all function keys
                    return 1;
                }
                if (key == Keys.Q && (Control.ModifierKeys & Keys.Control) != 0)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                // Special case for Ctrl+Alt+Del - this can't be completely blocked
                // but we'll try to suppress it when we can
                if (key == Keys.Delete &&
                    (Control.ModifierKeys & (Keys.Control | Keys.Alt)) == (Keys.Control | Keys.Alt))
                {
                    return 1; // Attempt to block but might not work
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void DisableSystemKeys()
        {
            try
            {
                // This API tricks Windows into thinking a screensaver is running,
                // which disables certain system key combinations including Alt+Tab
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 1, nint.Zero, SPIF_SENDCHANGE);

                // Disable Windows ghosting which can occur with certain key combinations
                DisableProcessWindowsGhosting();

                // Try a more aggressive approach to disable Alt+Tab
                // Get all open forms and register Alt+Tab hotkey on all of them
                foreach (Form form in Application.OpenForms)
                {
                    // Register Alt+Tab as a hotkey to capture it
                    RegisterHotKey(form.Handle, 1, MOD_ALT, VK_TAB);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling system keys: {ex.Message}");
            }
        }

        private static void EnableSystemKeys()
        {
            try
            {
                // Restore normal key behavior
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 0, nint.Zero, SPIF_SENDCHANGE);

                // Unregister all hotkeys
                foreach (Form form in Application.OpenForms)
                {
                    UnregisterHotKey(form.Handle, 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling system keys: {ex.Message}");
            }
        }

        private static void DisableGameBarSystemWide()
        {
            try
            {
                // HKEY_LOCAL_MACHINE is a system-wide policy and requires admin rights.
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
                {
                    // A value of 0 explicitly tells the system to disable the Game Bar.
                    key?.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Game Bar system-wide: {ex.Message}");
            }
        }

        // This method cleans up after the application exits.
        public static void EnableGameBarSystemWide()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", true))
                {
                    if (key != null && key.GetValue("AllowGameDVR") != null)
                    {
                        key.DeleteValue("AllowGameDVR");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Game Bar system-wide: {ex.Message}");
            }
        }

        #endregion Keyboard Hook and System Keys
    }
}