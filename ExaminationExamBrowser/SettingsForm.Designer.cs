namespace ExamBrowserV2
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            cmsUrlTextBox = new TextBox();
            passwordTextBox = new TextBox();
            saveButton = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(64, 166);
            label1.Name = "label1";
            label1.Size = new Size(69, 20);
            label1.TabIndex = 0;
            label1.Text = "CMS URL";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(64, 224);
            label2.Name = "label2";
            label2.Size = new Size(118, 20);
            label2.TabIndex = 1;
            label2.Text = "Admin Password";
            // 
            // cmsUrlTextBox
            // 
            cmsUrlTextBox.Location = new Point(292, 166);
            cmsUrlTextBox.Name = "cmsUrlTextBox";
            cmsUrlTextBox.Size = new Size(364, 27);
            cmsUrlTextBox.TabIndex = 2;
            // 
            // passwordTextBox
            // 
            passwordTextBox.Location = new Point(292, 224);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.Size = new Size(364, 27);
            passwordTextBox.TabIndex = 3;
            // 
            // saveButton
            // 
            saveButton.Location = new Point(318, 328);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(94, 29);
            saveButton.TabIndex = 4;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += saveButton_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(saveButton);
            Controls.Add(passwordTextBox);
            Controls.Add(cmsUrlTextBox);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "SettingsForm";
            Text = "Initial Configuration";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox cmsUrlTextBox;
        private TextBox passwordTextBox;
        private Button saveButton;
    }
}