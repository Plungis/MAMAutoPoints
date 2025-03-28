using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32; // For registry access

// Alias to resolve ambiguous Timer reference.
using Timer = System.Windows.Forms.Timer;

namespace MAMAutoPoints
{
    public class MainForm : Form
    {
        private TextBox textBoxLog = null!;
        private TextBox textBoxPointsBuffer = null!;
        private CheckBox checkBoxBuyVip = null!;
        private TextBox textBoxNextRun = null!;
        // Cookie settings will be in their own group box.
        private Button buttonBrowseCookie = null!;
        private Button buttonEditCookie = null!;
        private Button buttonCreateCookie = null!;
        private Label labelTotalGB = null!;
        private Label labelCumulativePointsValue = null!;
        private Label labelNextRunCountdown = null!;
        private Button buttonRun = null!;
        private Button buttonPause = null!;
        private Button buttonExit = null!;
        private Button buttonHelpCookie = null!;
        private Timer timerCountdown = null!;
        private DateTime? nextRunTime = null;
        private int cumulativePointsSpent = 0;
        private int cumulativeUploadGB = 0;

        // User Information controls.
        private GroupBox groupBoxUserInfo = null!;
        private Label labelUserName = null!;
        private Label labelVipExpires = null!;
        private Label labelDownloaded = null!;
        private Label labelUploaded = null!;
        private Label labelRatio = null!;

        private bool automationRunning = false;
        private bool paused = false;

        // NotifyIcon for system tray functionality.
        private NotifyIcon notifyIcon = null!;

        // Field to control whether minimizing hides the form.
        private bool enableMinimizeToTray = true;

        // CheckBoxes for system settings.
        private CheckBox autoStartCheckBox = null!;
        private CheckBox minimizeToTrayCheckBox = null!;

        // Cookie file text box (moved to its own group).
        private TextBox textBoxCookieFile = null!;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Set the ClientSize to a tight fit so that the gap at the bottom is minimal.
            this.ClientSize = new Size(800, 700);
            this.Text = "MAM Auto Points";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // Log TextBox.
            textBoxLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Location = new Point(10, 10),
                Size = new Size(760, 200)
            };
            this.Controls.Add(textBoxLog);

            // User Information Group.
            groupBoxUserInfo = new GroupBox
            {
                Text = "User Information",
                Location = new Point(10, 220),
                Size = new Size(760, 120),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxUserInfo);

            var labelUserNameTitle = new Label
            {
                Text = "Username:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxUserInfo.Controls.Add(labelUserNameTitle);

            labelUserName = new Label
            {
                Text = "N/A",
                Location = new Point(100, 25),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxUserInfo.Controls.Add(labelUserName);

            var labelVipExpiresTitle = new Label
            {
                Text = "VIP Expires:",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            groupBoxUserInfo.Controls.Add(labelVipExpiresTitle);

            labelVipExpires = new Label
            {
                Text = "N/A",
                Location = new Point(100, 50),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            groupBoxUserInfo.Controls.Add(labelVipExpires);

            var labelDownloadedTitle = new Label
            {
                Text = "Downloaded:",
                Location = new Point(10, 75),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelDownloadedTitle);

            labelDownloaded = new Label
            {
                Text = "N/A",
                Location = new Point(100, 75),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelDownloaded);

            var labelUploadedTitle = new Label
            {
                Text = "Uploaded:",
                Location = new Point(380, 25),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelUploadedTitle);

            labelUploaded = new Label
            {
                Text = "N/A",
                Location = new Point(480, 25),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelUploaded);

            var labelRatioTitle = new Label
            {
                Text = "Ratio:",
                Location = new Point(380, 50),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxUserInfo.Controls.Add(labelRatioTitle);

            labelRatio = new Label
            {
                Text = "N/A",
                Location = new Point(480, 50),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxUserInfo.Controls.Add(labelRatio);

            // Divider between User Information and Row 1.
            Panel dividerUserInfo = new Panel
            {
                Location = new Point(10, groupBoxUserInfo.Bottom + 10),
                Size = new Size(760, 2),
                BackColor = Color.Gray
            };
            this.Controls.Add(dividerUserInfo);

            // --- Row 1: Two columns (General Settings and Totals) ---
            int row1Y = dividerUserInfo.Bottom + 10; // e.g., 10 pixels below the divider.

            // Left column: General Settings.
            var groupBoxSettings = new GroupBox
            {
                Text = "General Settings",
                Location = new Point(10, row1Y),
                Size = new Size(380, 170),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxSettings);

            int labelX = 10;
            int textBoxX = 150;

            checkBoxBuyVip = new CheckBox
            {
                Text = "Buy Max VIP?",
                Location = new Point(labelX, 20),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.LightGreen
            };
            groupBoxSettings.Controls.Add(checkBoxBuyVip);

            var labelPointsBufferPrefix = new Label
            {
                Text = "Points Buffer:",
                Location = new Point(labelX, 50),
                AutoSize = true,
                ForeColor = Color.LightBlue,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupBoxSettings.Controls.Add(labelPointsBufferPrefix);

            textBoxPointsBuffer = new TextBox
            {
                Location = new Point(textBoxX, 50),
                Size = new Size(100, 23),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Text = "10000"
            };
            groupBoxSettings.Controls.Add(textBoxPointsBuffer);

            var labelNextRun = new Label
            {
                Text = "Next Run Delay (hours):",
                Location = new Point(labelX, 80),
                AutoSize = true,
                ForeColor = Color.Plum,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupBoxSettings.Controls.Add(labelNextRun);

            textBoxNextRun = new TextBox
            {
                Location = new Point(textBoxX, 80),
                Size = new Size(100, 23),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Text = "12"
            };
            groupBoxSettings.Controls.Add(textBoxNextRun);

            // Right column: Totals.
            var groupBoxTotals = new GroupBox
            {
                Text = "Totals",
                Location = new Point(400, row1Y),
                Size = new Size(380, 170),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxTotals);

            var labelGBBought = new Label
            {
                Text = "Total GB Bought:",
                Location = new Point(10, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelGBBought);

            labelTotalGB = new Label
            {
                Text = "0",
                Location = new Point(180, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelTotalGB);

            var labelCumulativePoints = new Label
            {
                Text = "Cumulative Points Spent:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelCumulativePoints);

            labelCumulativePointsValue = new Label
            {
                Text = "0",
                Location = new Point(180, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelCumulativePointsValue);

            var labelNextRunIn = new Label
            {
                Text = "Next Run In:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunIn);

            labelNextRunCountdown = new Label
            {
                Text = "",
                Location = new Point(180, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunCountdown);

            // Divider between Row 1 and Row 2.
            Panel dividerRow = new Panel
            {
                Location = new Point(10, row1Y + 170 + 10),
                Size = new Size(770, 2),
                BackColor = Color.Gray
            };
            this.Controls.Add(dividerRow);

            // --- Row 2: Two columns (System Settings and Cookie Settings) ---
            int row2Y = dividerRow.Bottom + 10;

            // Left column: System Settings.
            var groupBoxSystemSettings = new GroupBox
            {
                Text = "System Settings",
                Location = new Point(10, row2Y),
                Size = new Size(380, 100),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxSystemSettings);

            autoStartCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(labelX, 25),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            autoStartCheckBox.CheckedChanged += AutoStartCheckBox_CheckedChanged;
            groupBoxSystemSettings.Controls.Add(autoStartCheckBox);

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to System Tray",
                Location = new Point(200, 25),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.LightGreen
            };
            minimizeToTrayCheckBox.CheckedChanged += MinimizeToTrayCheckBox_CheckedChanged;
            groupBoxSystemSettings.Controls.Add(minimizeToTrayCheckBox);

            // Right column: Cookie Settings.
            var groupBoxCookieSettings = new GroupBox
            {
                Text = "Cookie Settings",
                Location = new Point(400, row2Y),
                Size = new Size(380, 100),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxCookieSettings);

            var labelCookieFile = new Label
            {
                Text = "Cookies File:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.Orange
            };
            groupBoxCookieSettings.Controls.Add(labelCookieFile);

            textBoxCookieFile = new TextBox
            {
                Location = new Point(110, 22),
                Size = new Size(200, 23),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Text = ""
            };
            groupBoxCookieSettings.Controls.Add(textBoxCookieFile);

            // Arrange cookie buttons on a second row.
            buttonBrowseCookie = new Button
            {
                Text = "Select File",
                Location = new Point(10, 60),
                Size = new Size(100, 30),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonBrowseCookie.Click += ButtonBrowseCookie_Click;
            groupBoxCookieSettings.Controls.Add(buttonBrowseCookie);

            buttonEditCookie = new Button
            {
                Text = "Edit Cookie",
                Location = new Point(120, 60),
                Size = new Size(100, 30),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonEditCookie.Click += ButtonEditCookie_Click;
            groupBoxCookieSettings.Controls.Add(buttonEditCookie);

            buttonCreateCookie = new Button
            {
                Text = "Create my Cookie!",
                Location = new Point(230, 60),
                Size = new Size(120, 30),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonCreateCookie.Click += ButtonCreateCookie_Click;
            groupBoxCookieSettings.Controls.Add(buttonCreateCookie);

            // --- Bottom Action Buttons ---
            // Position these just below Row 2 with a small gap.
            int bottomY = groupBoxCookieSettings.Bottom + 10;

            buttonRun = new Button
            {
                Text = "Run Script",
                Location = new Point(10, bottomY),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonRun.Click += ButtonRun_Click;
            this.Controls.Add(buttonRun);

            buttonPause = new Button
            {
                Text = "Pause",
                Location = new Point(120, bottomY),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonPause.Click += ButtonPause_Click;
            this.Controls.Add(buttonPause);

            buttonExit = new Button
            {
                Text = "Exit",
                Location = new Point(230, bottomY),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonExit.Click += ButtonExit_Click;
            this.Controls.Add(buttonExit);

            buttonHelpCookie = new Button
            {
                Text = "Instructions",
                Location = new Point(340, bottomY),
                Size = new Size(150, 30),
                Anchor = AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonHelpCookie.Click += ButtonHelpCookie_Click;
            this.Controls.Add(buttonHelpCookie);

            timerCountdown = new Timer { Interval = 1000 };
            timerCountdown.Tick += TimerCountdown_Tick;
            timerCountdown.Start();

            // Setup system tray NotifyIcon.
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "MAM Auto Points"
            };
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        // Only minimize to tray if enabled.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized && enableMinimizeToTray)
            {
                this.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(3000, "MAM Auto Points", "Application minimized to tray.", ToolTipIcon.Info);
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void ButtonBrowseCookie_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    textBoxCookieFile.Text = ofd.FileName;
                }
            }
        }

        private void ButtonEditCookie_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(textBoxCookieFile.Text) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open cookie file. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonHelpCookie_Click(object? sender, EventArgs e)
        {
            string instructions = "MAM Website Instructions:\n" +
                "1. Log in to your Myanonamouse account and go to Preferences => Security.\n" +
                "2. Enter your public IP address and click 'Submit Changes'.\n" +
                "3. Copy the security string displayed.\n" +
                "4. Use 'Create my Cookie!' to save your session cookie.\n\n" +
                "Program Instructions:\n" +
                "1. Ensure your cookie file path is entered in the Cookie Settings box.\n" +
                "2. Adjust your Points Buffer and Next Run Delay as needed.\n" +
                "3. Click 'Run Script' to start the automation.";
            MessageBox.Show(instructions, "Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonCreateCookie_Click(object? sender, EventArgs e)
        {
            string uniqueId = Microsoft.VisualBasic.Interaction.InputBox("Enter the unique security string from your account:", "Create Cookie File!", "");
            if (!string.IsNullOrEmpty(uniqueId))
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*";
                    sfd.Title = "Save Cookie File";
                    sfd.FileName = "MAM.cookies";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(sfd.FileName, uniqueId);
                        textBoxCookieFile.Text = sfd.FileName;
                        MessageBox.Show("Cookie file created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ButtonRun_Click(object? sender, EventArgs e)
        {
            if (paused)
            {
                paused = false;
                buttonPause.Text = "Pause";
                AppendLog("Resuming automation.");
            }
            if (!int.TryParse(textBoxPointsBuffer.Text, out int pointsBuffer))
            {
                MessageBox.Show("Please enter a valid Points Buffer value.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(textBoxNextRun.Text, out int nextRunHours))
            {
                MessageBox.Show("Please enter a valid Next Run Delay in hours.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool vipEnabled = checkBoxBuyVip.Checked;
            string cookieFile = textBoxCookieFile.Text;

            if (automationRunning)
            {
                AppendLog("Automation is already running.");
                return;
            }

            Task.Run(async () =>
            {
                automationRunning = true;
                await AutomationService.RunAutomationAsync(cookieFile, pointsBuffer, vipEnabled, nextRunHours,
                    AppendLog, UpdateUserInformation, UpdateTotals);
                automationRunning = false;
                // Schedule the next run.
                nextRunTime = DateTime.Now.AddHours(nextRunHours);
            });
        }

        private void ButtonPause_Click(object? sender, EventArgs e)
        {
            paused = !paused;
            buttonPause.Text = paused ? "Resume" : "Pause";
            AppendLog(paused ? "Automation paused." : "Automation resumed.");
        }

        private void ButtonExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void TimerCountdown_Tick(object? sender, EventArgs e)
        {
            if (nextRunTime.HasValue)
            {
                if (paused)
                {
                    labelNextRunCountdown.Text = "Paused";
                    return;
                }
                TimeSpan remaining = nextRunTime.Value - DateTime.Now;
                labelNextRunCountdown.Text = remaining.TotalSeconds > 0 ?
                    string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds) :
                    "Ready";

                if (remaining.TotalSeconds <= 0 && !automationRunning)
                {
                    nextRunTime = null;
                    ButtonRun_Click(sender, e);
                }
            }
            else
            {
                labelNextRunCountdown.Text = "";
            }
        }

        private void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendLog), message);
                return;
            }
            textBoxLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void UpdateUserInformation(AutomationService.UserSummary summary)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<AutomationService.UserSummary>(UpdateUserInformation), summary);
                return;
            }
            labelUserName.Text = summary.Username ?? "N/A";
            labelVipExpires.Text = summary.VipExpires;
            labelDownloaded.Text = summary.Downloaded;
            labelUploaded.Text = summary.Uploaded;
            labelRatio.Text = summary.Ratio;
        }

        private void UpdateTotals(int totalGB, int pointsSpent)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, int>(UpdateTotals), totalGB, pointsSpent);
                return;
            }
            cumulativeUploadGB += totalGB;
            labelTotalGB.Text = cumulativeUploadGB.ToString();
            cumulativePointsSpent += pointsSpent;
            labelCumulativePointsValue.Text = cumulativePointsSpent.ToString();
        }

        // Event handler for the Auto-Start CheckBox.
        private void AutoStartCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox chk)
            {
                if (chk.Checked)
                {
                    EnableAutoStart();
                }
                else
                {
                    DisableAutoStart();
                }
            }
        }

        // Event handler for the Minimize-to-Tray CheckBox.
        private void MinimizeToTrayCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox chk)
            {
                enableMinimizeToTray = chk.Checked;
                AppendLog("Minimize to System Tray " + (chk.Checked ? "enabled." : "disabled."));
            }
        }

        private void EnableAutoStart()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    key.SetValue("MAMAutoPoints", exePath);
                    AppendLog("Auto Start enabled.");
                }
                else
                {
                    AppendLog("Registry key not found. Auto Start not enabled.");
                }
            }
            catch (Exception ex)
            {
                AppendLog("Error enabling Auto Start: " + ex.Message);
            }
        }

        private void DisableAutoStart()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null && key.GetValue("MAMAutoPoints") != null)
                {
                    key.DeleteValue("MAMAutoPoints", false);
                    AppendLog("Auto Start disabled.");
                }
                else
                {
                    AppendLog("Auto Start entry not found.");
                }
            }
            catch (Exception ex)
            {
                AppendLog("Error disabling Auto Start: " + ex.Message);
            }
        }
    }
}
