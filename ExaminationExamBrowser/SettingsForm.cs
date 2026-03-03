using System.Text.Json;

namespace ExamBrowserV2
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cmsUrlTextBox.Text) || string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show("All fields are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Define the structure of our settings file
                var appSettings = new
                {
                    AppSettings = new
                    {
                        AdminPassword = passwordTextBox.Text,
                        CMS_Url = cmsUrlTextBox.Text,
                        // Keep a placeholder for Exam_Url if needed
                        Exam_Url = ""
                    }
                };

                // Get the path to appsettings.json in the user's local app data
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appFolder = Path.Combine(appDataPath, "ExamBrowser"); // A dedicated folder for our app
                Directory.CreateDirectory(appFolder); // Ensure the folder exists
                string settingsFilePath = Path.Combine(appFolder, "appsettings.json");

                // Serialize the object to a JSON string
                string json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });

                // Write the string to the file
                File.WriteAllText(settingsFilePath, json);

                MessageBox.Show("Settings saved successfully. The application will now start.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}