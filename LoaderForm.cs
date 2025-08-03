using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeyAuth;

namespace FireLoader
{
    public class LoaderForm : Form
    {
        public static api KeyAuthApp = new api(
            name: "FireLoader",
            ownerid: "53mGxqHsrc",
            secret: "826a291e1a098ee14a90ba5e9613feeabcf70c02b40440278193f7862377bbe0",
            version: "1.0"
        );

        private TextBox keyBox;
        private CheckBox rememberMeBox;
        private CheckBox saveKeyBox;
        private Label loaderStatusLabel;
        private Label keyStatusLabel;
        private Label serverStatusLabel;
        private Button loginButton;
        private Button toggleEyeButton;

        private Panel titleBar;
        private Label titleLabel;
        private Button closeButton;
        private Button minimizeButton;
        private Label subTimerLabel;
        private System.Windows.Forms.Timer subTimer;
        private DateTime subExpiryTime;
        private System.Windows.Forms.Timer subscriptionTimer;
        private DateTime subscriptionExpiry;
        private Label versionStatusLabel;



        private Panel gameCardsPanel;

        private bool loaderIsBusy = false;

        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        private const string ConfigPath = @"%AppData%\FireLoader\config.dat";

        private readonly string currentVersion;


        private System.Windows.Forms.Timer statusUpdateTimer;
        private bool isUpdating = false;
        private const string updateJsonUrl = "https://raw.githubusercontent.com/SilentByte-sys/SBMultiLoader/main/update.json";


        public LoaderForm()
        {
            var version = Application.ProductVersion;
            var parts = version.Split('.');
            currentVersion = $"{parts[0]}.{parts[1]}.{parts[2]}"; // major.minor.build

            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(25, 25, 25);
            ClientSize = new Size(520, 420); // Extra height for gamecards
            StartPosition = FormStartPosition.CenterScreen;

            InitTitleBar();
            InitUI();
            LoadSavedCreds();

            // Set version label here (make sure loaderVersionLabel is initialized in InitUI or earlier)
            versionStatusLabel.Text = $"Loader Version: {currentVersion}";

            KeyAuthApp.init();
            _ = CheckServerStatus();
            StartStatusUpdateTimer();
            _ = CheckForUpdatesAsync();
        }


        private void InitTitleBar()
        {
            titleBar = new Panel
            {
                BackColor = Color.FromArgb(40, 40, 40),
                Height = 30,
                Dock = DockStyle.Top,
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            Controls.Add(titleBar);

            titleLabel = new Label
            {
                Text = "🔥 FireLoader | Phantom State RP",
                ForeColor = Color.FromArgb(0, 255, 200),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(10, 5),
                AutoSize = true,
            };
            titleBar.Controls.Add(titleLabel);

            closeButton = new Button
            {
                Text = "X",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 20, 60),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 30),
                Location = new Point(ClientSize.Width - 40, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => Application.Exit();
            titleBar.Controls.Add(closeButton);

            minimizeButton = new Button
            {
                Text = "–",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 30),
                Location = new Point(ClientSize.Width - 80, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(minimizeButton);
        }

        private void InitUI()
        {
            var statusPanel = new Panel
            {
                BackColor = Color.FromArgb(40, 40, 40),
                Size = new Size(175, ClientSize.Height - titleBar.Height),
                Location = new Point(0, titleBar.Height),
                Padding = new Padding(15),
            };
            Controls.Add(statusPanel);
            InitSubscriptionTimerUI(statusPanel);

            var statusTitle = new Label
            {
                Text = "📡 Status",
                ForeColor = Color.FromArgb(0, 255, 200),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10),
            };
            statusPanel.Controls.Add(statusTitle);

            serverStatusLabel = CreateStatusLabel("Server Status: Checking...");
            serverStatusLabel.Location = new Point(10, 50);
            statusPanel.Controls.Add(serverStatusLabel);

            loaderStatusLabel = CreateStatusLabel("Loader Status: Idle");
            loaderStatusLabel.Location = new Point(10, 90);
            statusPanel.Controls.Add(loaderStatusLabel);

            keyStatusLabel = CreateStatusLabel("Key Status: Unknown");
            keyStatusLabel.Location = new Point(10, 130);
            statusPanel.Controls.Add(keyStatusLabel);

            versionStatusLabel = CreateStatusLabel($"Loader Version: {currentVersion}");
            versionStatusLabel.Location = new Point(10, 210);
            statusPanel.Controls.Add(versionStatusLabel);

            // Right side form elements
            int formX = 210;
            int inputWidth = 250;
            int inputHeight = 32;

            var keyLabel = new Label
            {
                Text = "License Key",
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(formX, 40),
                AutoSize = true,
            };
            Controls.Add(keyLabel);

            keyBox = new TextBox
            {
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Size = new Size(inputWidth, inputHeight),
                Location = new Point(formX, 65),
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };
            Controls.Add(keyBox);

            toggleEyeButton = new Button
            {
                Size = new Size(30, 30),
                Location = new Point(formX + inputWidth + 5, 65),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(0, 255, 200),
                Cursor = Cursors.Hand,
                Text = "👁",
            };
            toggleEyeButton.FlatAppearance.BorderSize = 0;
            toggleEyeButton.Click += (s, e) =>
            {
                keyBox.UseSystemPasswordChar = !keyBox.UseSystemPasswordChar;
                toggleEyeButton.Text = keyBox.UseSystemPasswordChar ? "👁" : "🚫";
            };
            Controls.Add(toggleEyeButton);

            rememberMeBox = new CheckBox
            {
                Text = "Remember Me",
                Location = new Point(formX, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            Controls.Add(rememberMeBox);

            saveKeyBox = new CheckBox
            {
                Text = "Save Key",
                Location = new Point(formX + 140, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            Controls.Add(saveKeyBox);

            loginButton = new Button
            {
                Text = "Connect",
                Size = new Size(inputWidth, 40),
                Location = new Point(formX, 155),
                BackColor = Color.FromArgb(0, 255, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.MouseEnter += (s, e) => loginButton.BackColor = Color.FromArgb(0, 200, 160);
            loginButton.MouseLeave += (s, e) => loginButton.BackColor = Color.FromArgb(0, 255, 200);
            loginButton.Click += async (s, e) => await LoginAsync();
            Controls.Add(loginButton);

            // Gamecards panel - hidden initially
            gameCardsPanel = new Panel
            {
                Location = new Point(formX, 210),
                Size = new Size(inputWidth, 160),
                BackColor = Color.FromArgb(20, 20, 20),
                Visible = false,
            };
            Controls.Add(gameCardsPanel);
        }
        private void InitSubscriptionTimerUI(Panel statusPanel)
        {
            subTimerLabel = CreateStatusLabel("Sub Expiry: Unknown");
            subTimerLabel.Location = new Point(10, 170);
            statusPanel.Controls.Add(subTimerLabel);
        }
        private void SubTimer_Tick(object sender, EventArgs e)
        {
            UpdateSubTimerLabel();
        }

        private void UpdateSubTimerLabel()
        {
            TimeSpan remaining = subExpiryTime - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                subTimerLabel.Text = "Sub Expiry: Expired ❌";
                subTimer.Stop();
            }
            else
            {
                subTimerLabel.Text = $"Sub Expiry: {remaining.Days}d {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }


        private void LaunchGame(string game)
        {
            switch (game)
            {
                case "FiveM":
                    LaunchFiveMServer();
                    break;
                case "CS2":
                    LaunchCS2();
                    break;
                default:
                    MessageBox.Show("Game launch not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private Label CreateStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                AutoSize = true,
            };
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void LoadSavedCreds()
        {
            try
            {
                if (File.Exists(Environment.ExpandEnvironmentVariables(ConfigPath)))
                {
                    var json = Unprotect(File.ReadAllBytes(Environment.ExpandEnvironmentVariables(ConfigPath)));
                    var creds = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                    if (creds != null)
                    {
                        rememberMeBox.Checked = !string.IsNullOrEmpty(creds.GetValueOrDefault("Username", ""));
                        saveKeyBox.Checked = !string.IsNullOrEmpty(creds.GetValueOrDefault("Key", ""));
                        keyBox.Text = creds.GetValueOrDefault("Key", "");
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private byte[] Protect(string plain) =>
            ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);

        private string Unprotect(byte[] encrypted) =>
            Encoding.UTF8.GetString(ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));

        private async Task<bool> TryLicenseKeyAsync(string key)
        {
            await KeyAuthApp.license(key);
            return KeyAuthApp.response?.success ?? false;
        }

        private async Task LoginAsync()
        {
            if (loaderIsBusy) return;
            loaderIsBusy = true;

            loaderStatusLabel.Text = "Loader Status: Authenticating...";
            keyStatusLabel.Text = "Key Status: Checking...";

            bool loginSuccess = false;

            if (!string.IsNullOrWhiteSpace(keyBox.Text))
            {
                loginSuccess = await TryLicenseKeyAsync(keyBox.Text);
            }
            else
            {
                MessageBox.Show("Please enter your license key.", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                loaderIsBusy = false;
                return;
            }

            loaderIsBusy = false;

            if (!loginSuccess)
            {
                string failReason = KeyAuthApp.response?.message ?? "Unknown error";

                if (failReason.Contains("paused", StringComparison.OrdinalIgnoreCase))
                    failReason = "Your subscription is paused. Contact support.";
                else if (failReason.Contains("hwid", StringComparison.OrdinalIgnoreCase))
                    failReason = "This key is locked to another PC. Contact support to reset.";
                else if (failReason.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                    failReason = "Invalid license key.";
                else if (failReason.Contains("expired", StringComparison.OrdinalIgnoreCase))
                    failReason = "Your license key has expired.";

                loaderStatusLabel.Text = "Loader Status: Failed ❌";
                keyStatusLabel.Text = "Key Status: " + failReason;

                MessageBox.Show(failReason, "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (KeyAuthApp.user_data?.subscriptions == null || KeyAuthApp.user_data.subscriptions.Count == 0)
            {
                MessageBox.Show("No active subscription found for this key.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sub = KeyAuthApp.user_data.subscriptions[0];
            if (long.TryParse(sub.expiry, out long expiryUnix))
            {
                DateTime expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime;
                if (expiryDate < DateTime.UtcNow)
                {
                    MessageBox.Show("Your subscription has expired.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 🚀 Start dynamic countdown in status panel
                 StartSubscriptionCountdown(expiryDate);
            }

            loaderStatusLabel.Text = "Loader Status: Success ✅";
            keyStatusLabel.Text = "Key Status: Valid ✅";

            SaveCreds();

            string allowedGame = GetAllowedGameFromSubscription();
            if (allowedGame == "Unknown")
            {
                MessageBox.Show("Your license does not allow any supported games.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Hide login UI controls
            ToggleLoginUI(false);

            ShowGameCard(allowedGame);
        }

        private string GetAllowedGameFromSubscription()
        {
            if (KeyAuthApp.user_data?.subscriptions == null || KeyAuthApp.user_data.subscriptions.Count == 0)
                return "Unknown";

            string subName = KeyAuthApp.user_data.subscriptions[0].subscription.ToLower();

            if (subName.Contains("fivem")) return "FiveM";
            if (subName.Contains("cs2")) return "CS2";
            if (subName.Contains("fortnite")) return "Fortnite";

            return "Unknown";
        }

        private void ShowGameCard(string game)
        {
            gameCardsPanel.Controls.Clear();
            gameCardsPanel.Visible = true;

            var card = CreateGameCard(game, new Point(10, 10));
            gameCardsPanel.Controls.Add(card);
        }

        private void ToggleLoginUI(bool visible)
        {
            keyBox.Visible = visible;
            toggleEyeButton.Visible = visible;
            loginButton.Visible = visible;
            rememberMeBox.Visible = visible;
            saveKeyBox.Visible = visible;
        }

        private string ExtractGameFromKey(string key)
        {
            try
            {
                var parts = key.Split('-');
                if (parts.Length > 1)
                {
                    string gamePart = parts[1].ToLower();

                    if (gamePart.Contains("fivem")) return "FiveM";
                    if (gamePart.Contains("cs2")) return "CS2";
                    if (gamePart.Contains("fortnite")) return "Fortnite";
                }
            }
            catch { }
            return "Unknown";
        }

        private void ShowAvailableGames()
        {
            gameCardsPanel.Controls.Clear();
            gameCardsPanel.Visible = true;

            int yOffset = 10;
            int spacing = 140; // card height + some padding

            var allowedGames = new List<string>();

            string key = keyBox.Text;
            if (key.Contains("FiveM", StringComparison.OrdinalIgnoreCase))
                allowedGames.Add("FiveM");
            if (key.Contains("CS2", StringComparison.OrdinalIgnoreCase))
                allowedGames.Add("CS2");

            foreach (var game in allowedGames)
            {
                var card = CreateGameCard(game, new Point(10, yOffset));
                gameCardsPanel.Controls.Add(card);
                yOffset += spacing;
            }
        }

        private Panel CreateGameCard(string game, Point location)
        {
            var card = new Panel
            {
                Size = new Size(gameCardsPanel.Width - 20, 120),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Tag = game,
                Location = location
            };

            card.Paint += (s, e) =>
            {
                int radius = 20;
                var rect = card.ClientRectangle;
                using (var path = GetRoundedRectPath(rect, radius))
                using (var brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            };

            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", $"{game}.png");
            var pic = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Tag = game,
            };

            if (File.Exists(imgPath))
                pic.Image = Image.FromFile(imgPath);
            else
                pic.BackColor = Color.DimGray;

            pic.Click += (s, e) => LaunchGame((string)((PictureBox)s).Tag);
            card.Click += (s, e) => LaunchGame((string)((Panel)s).Tag);

            var lbl = new Label
            {
                Text = game,
                ForeColor = Color.FromArgb(0, 255, 200),
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(120, 40),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Tag = game,
            };
            lbl.Click += (s, e) => LaunchGame((string)((Label)s).Tag);

            card.Controls.Add(pic);
            card.Controls.Add(lbl);

            return card;
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LaunchFiveMServer()
        {
            string serverJoinUrl = "https://cfx.re/join/zxpb3d";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = serverJoinUrl,
                    UseShellExecute = true
                });
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch FiveM server link:\n{ex.Message}");
            }
        }

        private void LaunchCS2()
        {
            try
            {
                // Steam app ID for CS2 is 730
                Process.Start(new ProcessStartInfo("steam://rungameid/730") { UseShellExecute = true });
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch CS2:\n{ex.Message}");
            }
        }

        private void SaveCreds()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(ConfigPath)));
                var creds = new
                {
                    Username = rememberMeBox.Checked ? "saved" : "",
                    Password = "",
                    Key = saveKeyBox.Checked ? keyBox.Text : ""
                };
                File.WriteAllBytes(Environment.ExpandEnvironmentVariables(ConfigPath), Protect(JsonSerializer.Serialize(creds)));
            }
            catch
            {
                // ignore
            }
        }

        private async Task CheckServerStatus()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetAsync("http://97.201.203.3:30120/info.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var info = JsonSerializer.Deserialize<FiveMServerInfo>(json);

                    int players = info.Data.Players.Count;
                    int maxPlayers = info.Data.SvMaxclients;

                    serverStatusLabel.Text = $"Server Status: Online ✅ ({players}/{maxPlayers} players)";
                }
            }
            catch
            {
                serverStatusLabel.Text = "Server Status: Offline ❌";
            }
        }
        private void StartStatusUpdateTimer()
        {
            statusUpdateTimer?.Stop();
            if (statusUpdateTimer != null)
                statusUpdateTimer.Tick -= StatusUpdateTimer_Tick;

            statusUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            statusUpdateTimer.Start();
        }

        private void UpdateLoaderStatus(string status)
        {
            loaderStatusLabel.Text = $"Loader Status: {status}";
        }

        private void StartSubscriptionCountdown(DateTime expiry)
        {
            subscriptionExpiry = expiry;

            if (subscriptionTimer != null)
            {
                subscriptionTimer.Stop();
                subscriptionTimer.Tick -= SubscriptionTimer_Tick;
            }

            subscriptionTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // update every second
            };
            subscriptionTimer.Tick += SubscriptionTimer_Tick;
            subscriptionTimer.Start();
        }

        private void UpdateSubscriptionCountdown()
        {
            if (KeyAuthApp.user_data?.subscriptions?.Count > 0 &&
                long.TryParse(KeyAuthApp.user_data.subscriptions[0].expiry, out long expiryUnix))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime;
                var remaining = expiry - DateTime.UtcNow;

                if (remaining.TotalSeconds > 0)
                {
                    loaderStatusLabel.Text = $"Expires in: {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
                }
                else
                {
                    loaderStatusLabel.Text = "Expired ❌";
                }
            }
            else
            {
                loaderStatusLabel.Text = "No subscription data ❓";
            }
        }

        private readonly string latestVersionUrl = "https://raw.githubusercontent.com/SilentByte-sys/SBMultiLoader/refs/heads/main/latest_version.txt";
        private readonly string downloadUrl = "https://github.com/SilentByte-sys/SBMultiLoader/releases/latest/download/FireLoader.exe";

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();

                // Get latest version from GitHub text file
                string latestVersion = (await client.GetStringAsync(latestVersionUrl)).Trim();

                if (latestVersion != currentVersion)
                {
                    var result = MessageBox.Show(
                        $"A new version ({latestVersion}) is available. Update now?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    );

                    if (result == DialogResult.Yes)
                    {
                        string currentExe = Application.ExecutablePath;
                        string tempPath = Path.Combine(Path.GetTempPath(), "FireLoader_Update.exe");

                        // Download new EXE
                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                        File.WriteAllBytes(tempPath, fileBytes);

                        // Create updater batch file
                        string updaterPath = Path.Combine(Path.GetTempPath(), "update_loader.bat");
                        File.WriteAllText(updaterPath, $@"
@echo off
timeout /t 1 /nobreak > NUL
taskkill /F /IM {Path.GetFileName(currentExe)} > NUL
copy /Y ""{tempPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""{tempPath}""
del ""%~f0""
");

                        // Run updater script
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updaterPath,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });

                        Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update check failed: " + ex.Message);
            }
        }

        public class UpdateInfo
        {
            public string version { get; set; }
            public string download_url { get; set; }
        }

        private async void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            await CheckServerStatus();

            var keyStatus = KeyAuthApp.response?.success == true ? "Valid ✅" : "Invalid ❌";
            keyStatusLabel.Text = $"Key Status: {keyStatus}";

            UpdateSubscriptionCountdown();
        }

        private void SubscriptionTimer_Tick(object sender, EventArgs e)
        {
            var remaining = subscriptionExpiry - DateTime.UtcNow;

            if (remaining.TotalSeconds > 0)
            {
                loaderStatusLabel.Text = $"Expires in: {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s";
            }
            else
            {
                loaderStatusLabel.Text = "Subscription Expired ❌";
                subscriptionTimer.Stop();
            }
        }
    }

    public class FiveMServerInfo
    {
        public InfoData Data { get; set; }
    }

    public class InfoData
    {
        public int SvMaxclients { get; set; }
        public System.Collections.Generic.List<object> Players { get; set; }
    }
}
