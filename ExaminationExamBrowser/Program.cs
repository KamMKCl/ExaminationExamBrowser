using ExamBrowserV2;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ExaminationExamBrowser
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static bool AllowClose = false;

        [STAThread]
        private static void Main(string[] args)
        {

            var config = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            try
            {
                // Check if we're starting or exiting the exam
                bool isExiting = args.Length > 0 && args[0].Equals("--exit", StringComparison.OrdinalIgnoreCase);

                if (isExiting)
                {
                    // We're exiting the exam, restore normal settings
                    SecurityManager.DisableSecurity();
                }
                else
                {
                    // Register application exit event to restore settings
                    Application.ApplicationExit += (sender, e) =>
                    {
                        if (AllowClose)
                        {
                            // Only restore settings if the application is exiting properly
                            SecurityManager.DisableSecurity();
                        }
                    };

                    // We're starting the exam, apply security
                    SecurityManager.ApplySecurity();
                    Application.Run(new MainForm(config));
                }
            }
            catch (UnauthorizedAccessException)
            {
                RestartAsAdmin(args);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void RestartAsAdmin(string[] args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;

            // Pass along any arguments
            if (args.Length > 0)
            {
                startInfo.Arguments = string.Join(" ", args);
            }

            startInfo.Verb = "runas"; // This is what triggers the UAC prompt

            try
            {
                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User declined the UAC prompt
                MessageBox.Show("Administrative privileges are required to run this application properly.\n\nThe application will now exit.",
                                "Admin Rights Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }

            // Exit the current non-elevated instance
            Application.Exit();
        }
    }
}