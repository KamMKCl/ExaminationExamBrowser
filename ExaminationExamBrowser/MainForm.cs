using ExaminationExamBrowser;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Runtime.InteropServices;

namespace ExamBrowserV2
{
    public partial class MainForm : Form
    {
        private IConfiguration _config;
        private WebView2 webView;
        private Label loadingLabel; // NEW: Loading screen label

        // Import necessary Win32 API
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int WM_HOTKEY = 0x0312;

        public MainForm(IConfiguration config)
        {
            InitializeComponent();
            _config = config;

            // NEW: Set a dark background color to prevent the white flash
            this.BackColor = Color.FromArgb(32, 33, 36); // Dark gray/black

            // NEW: Create a professional loading text while the browser engine spins up
            loadingLabel = new Label
            {
                Text = "Loading Secure Browser...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(loadingLabel);

            // Read settings using the new config object
            string cmsUrl = (_config["AppSettings:CMS_Url"] ?? "about:blank").Trim();

            if (!cmsUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !cmsUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                cmsUrl = "http://" + cmsUrl;
            }
            string examUrl = (_config["AppSettings:Exam_Url"] ?? cmsUrl).Trim();

            if (!examUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !examUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                examUrl = "http://" + examUrl;
            }

            // Set form properties for kiosk mode
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.KeyPreview = true; // Essential for capturing keyboard events

            // Initialize WebView with the URL from config
            InitializeWebView(cmsUrl, examUrl);

            // Handle form closing to prevent Alt+F4
            this.FormClosing += MainForm_FormClosing;

            // Add activation handler to detect when form loses and regains focus
            this.Activated += MainForm_Activated;
            this.Deactivate += MainForm_Deactivate;

            // Create a timer to keep the form on top
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100; // Check every 100ms
            timer.Tick += (s, e) =>
            {
                // Force window to front periodically
                if (!this.TopMost)
                {
                    this.TopMost = true;
                    this.BringToFront();
                }
            };
            timer.Start();
        }

        // Override WndProc to handle hotkey messages
        protected override void WndProc(ref Message m)
        {
            // Process WM_HOTKEY messages
            if (m.Msg == WM_HOTKEY)
            {
                // Hotkey was pressed, but we intercept it here
                // 1 is the ID we registered for Alt+Tab
                if (m.WParam.ToInt32() == 1)
                {
                    // Alt+Tab was attempted, do nothing
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            SuspendLayout();
            // 
            // MainForm
            // 
            ClientSize = new Size(1067, 922);
            Cursor = Cursors.WaitCursor; // Use wait cursor while loading
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Margin = new Padding(4, 5, 4, 5);
            Name = "MainForm";
            Text = "Exam Browser";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            // When form is activated (gains focus), ensure it's in fullscreen
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            // When the form loses focus (e.g., after Alt+Ctrl+Del)
            // Force it back to the front
            this.TopMost = true;
            this.Activate();
            this.BringToFront();
            SetForegroundWindow(this.Handle);
        }

        private async void InitializeWebView(string CMS_url, string Exam_Url)
        {
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            webView.Visible = false; // NEW: Hide the browser while it's loading to prevent white flash
            webView.DefaultBackgroundColor = Color.FromArgb(32, 33, 36); // Match background
            this.Controls.Add(webView);

            try
            {
                // Initialize WebView2 environment with comprehensive security options
                var options = new CoreWebView2EnvironmentOptions();
                string insecureOrigins = $"{CMS_url},{Exam_Url}";

                string arguments =
                    $"--unsafely-treat-insecure-origin-as-secure={insecureOrigins} " +
                    "--use-fake-ui-for-media-stream " +
                    "--enable-features=InsecurePrivateNetwork " +
                    "--disable-web-security " +                    // Disable web security checks
                    "--allow-running-insecure-content " +          // Allow mixed content
                   "--disable-features=VizDisplayCompositor,CalculateNativeWinOcclusion,RendererBackgrounding " +// Prevent some security dialogs
                    "--ignore-ssl-errors " +                       // Ignore SSL certificate errors
                    "--ignore-certificate-errors " +               // Ignore certificate errors
                    "--ignore-urlfetcher-cert-requests " +         // Ignore cert requests
                    "--disable-extensions-http-throttling " +      // Disable throttling
                    "--disable-component-extensions-with-background-pages " +
                    "--no-first-run " +                           // Skip first run experience
                    "--disable-default-apps " +                   // Disable default apps
                    "--disable-background-timer-throttling";      // Disable background throttling

                options.AdditionalBrowserArguments = arguments;

                // 1. Get the path to the user's local app data folder
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                // 2. Create a specific folder for your application's data inside it
                string userDataFolder = Path.Combine(localAppData, "ExamBrowser");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                await webView.EnsureCoreWebView2Async(env);

                // Configure WebView2 settings
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.IsScriptEnabled = true;

                //Clear old session cookies every time the app opens
                await webView.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.Cookies);

                // IMPORTANT: Handle key press events - IMPORTANT FOR CTRL+Q
                webView.KeyDown += MainForm_KeyDown;

                // IMPORTANT: Handle certificate errors and security warnings
                webView.CoreWebView2.ServerCertificateErrorDetected += (sender, args) =>
                {
                    // Automatically accept all certificate errors
                    args.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
                };

                // Handle permission requests (camera, microphone, etc.)
                webView.CoreWebView2.PermissionRequested += (sender, args) =>
                {
                    args.State = CoreWebView2PermissionState.Allow;
                };

                // Handle navigation errors AND Hide Loading Screen
                webView.CoreWebView2.NavigationCompleted += (sender, args) =>
                {
                    // NEW: The exact millisecond the page finishes loading, hide the loading text and show the page!
                    loadingLabel.Visible = false;
                    webView.Visible = true;
                    this.Cursor = Cursors.Default; // Reset cursor back to normal arrow

                    if (!args.IsSuccess)
                    {
                        Console.WriteLine($"Navigation failed: {args.WebErrorStatus}");
                    }
                };

                // Handle new window requests to prevent popups
                webView.CoreWebView2.NewWindowRequested += (sender, args) =>
                {
                    args.Handled = true;
                };

                // Navigate to the URL
                webView.CoreWebView2.Navigate(CMS_url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing browser: {ex.Message}", "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for exit shortcut Ctrl+Q
            if (e.Control && e.KeyCode == Keys.Q)
            {
                // Show password form
                ShowPasswordForm();
                e.Handled = true;
                e.SuppressKeyPress = true; // Prevent the key from being processed further
                return;
            }

            // Block Ctrl+F just in case
            if (e.Control && e.KeyCode == Keys.F)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // Block Alt+F4
            if (e.Alt && e.KeyCode == Keys.F4)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

            // Block PrintScreen
            if (e.KeyCode == Keys.PrintScreen)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Only allow closing with specific exit code
            if (!Program.AllowClose)
                e.Cancel = true;
        }

        private void ShowPasswordForm()
        {
            string configuredPassword = _config["AppSettings:AdminPassword"];

            // If password is not configured, use a default for testing
            if (string.IsNullOrEmpty(configuredPassword))
            {
                configuredPassword = "admin"; // Default password for testing
            }

            // Create password form
            Form passwordForm = new Form
            {
                Width = 300,
                Height = 170,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Administrator Exit",
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label
            {
                Text = "Enter Password:",
                Left = 20,
                Top = 20,
                Width = 260
            };

            TextBox textBox = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 240,
                PasswordChar = '*'
            };

            Button confirmButton = new Button
            {
                Text = "Confirm",
                Left = 90,
                Top = 90,
                Width = 100,
                DialogResult = DialogResult.OK
            };

            confirmButton.Click += (sender, e) =>
            {
                if (textBox.Text == configuredPassword)
                {
                    Program.AllowClose = true;
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show("Incorrect password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                passwordForm.Close();
            };

            passwordForm.Controls.Add(label);
            passwordForm.Controls.Add(textBox);
            passwordForm.Controls.Add(confirmButton);
            passwordForm.AcceptButton = confirmButton;

            // Show the form as a dialog
            passwordForm.ShowDialog();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}