using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FireLoader
{
    public class SplashForm : Form
    {
        private PictureBox loadingGif;

        public SplashForm()
        {
            // Window setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.ClientSize = new Size(500, 400);

            // Create PictureBox
            loadingGif = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.StretchImage, // Fill the form
                Dock = DockStyle.Fill,
                BackColor = Color.Black // fallback if no image
            };

            // Path to loading.gif
            string gifPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "loading.gif");

            // Load GIF
            if (File.Exists(gifPath))
            {
                try
                {
                    loadingGif.Image = Image.FromFile(gifPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading loading.gif: " + ex.Message,
                        "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Loading GIF not found:\n" + gifPath,
                    "Missing File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Add to form
            this.Controls.Add(loadingGif);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Show splash for 6 seconds
            await Task.Delay(6000);

            // Show main loader form
            LoaderForm mainForm = new LoaderForm();
            mainForm.Show();

            // Hide splash
            this.Hide();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashForm));
            SuspendLayout();
            // 
            // SplashForm
            // 
            ClientSize = new Size(284, 261);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SplashForm";
            ResumeLayout(false);

        }
    }
}
