using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace MAMAutoPoints
{
    public class Form1 : Form
    {
        private const string MAM_API_ENDPOINT = "https://www.myanonamouse.net/jsonLoad.php";
        private const string POINTS_URL = "https://www.myanonamouse.net/json/bonusBuy.php/?spendtype=upload&amount=";
        private const string VIP_URL_TEMPLATE = "https://www.myanonamouse.net/json/bonusBuy.php/?spendtype=VIP&duration=max&_={timestamp}";

        private TextBox textBoxLog = null!;
        private TextBox textBoxPointsBuffer = null!;
        private CheckBox checkBoxBuyVip = null!;
        private TextBox textBoxNextRun = null!;
        private TextBox textBoxCookieFile = null!;
        private Button buttonBrowseCookie = null!;
        private Button buttonEditCookie = null!;
        private Label labelTotalGB = null!;
        private Label labelCumulativePointsValue = null!;
        private Label labelNextRunCountdown = null!;
        private Button buttonRun = null!;
        private Button buttonPause = null!;
        private Button buttonExit = null!;
        private Button buttonHelpCookie = null!;
        private Button buttonCreateCookie = null!;
        private System.Windows.Forms.Timer timerCountdown = null!;
        private DateTime? nextRunTime = null;
        private int cumulativePointsSpent = 0;

        private GroupBox groupBoxUserInfo = null!;
        private Label labelUserName = null!;
        private Label labelVipExpires = null!;
        private Label labelDownloaded = null!;
        private Label labelUploaded = null!;
        private Label labelRatio = null!;

        private bool automationRunning = false;
        private bool paused = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "MAM Auto Points";
            this.Size = new Size(800, 720);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

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

            // User Information Group
            groupBoxUserInfo = new GroupBox
            {
                Text = "User Information",
                Location = new Point(10, 220),
                Size = new Size(760, 120),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxUserInfo);

            Label labelUserNameTitle = new Label
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

            Label labelVipExpiresTitle = new Label
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

            Label labelDownloadedTitle = new Label
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

            Label labelUploadedTitle = new Label
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

            Label labelRatioTitle = new Label
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

            // Settings Group
            GroupBox groupBoxSettings = new GroupBox
            {
                Text = "Settings",
                Location = new Point(10, 360),
                Size = new Size(380, 180),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxSettings);

            // Left-align controls using fixed X coordinates
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

            Label labelPointsBufferPrefix = new Label
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

            Label labelNextRun = new Label
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

            Label labelCookieFile = new Label
            {
                Text = "Cookies File:",
                Location = new Point(labelX, 110),
                AutoSize = true,
                ForeColor = Color.Orange,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupBoxSettings.Controls.Add(labelCookieFile);

            textBoxCookieFile = new TextBox
            {
                Location = new Point(textBoxX, 110),
                Size = new Size(100, 23),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Text = ""
            };
            groupBoxSettings.Controls.Add(textBoxCookieFile);

            buttonBrowseCookie = new Button
            {
                Text = "Select File",
                Location = new Point(textBoxX + textBoxCookieFile.Width + 5, 110),
                Size = new Size(75, 23),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonBrowseCookie.Click += ButtonBrowseCookie_Click;
            groupBoxSettings.Controls.Add(buttonBrowseCookie);

            buttonEditCookie = new Button
            {
                Text = "Edit Cookie",
                Location = new Point(textBoxX, 140),
                Size = new Size(140, 30),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonEditCookie.Click += ButtonEditCookie_Click;
            groupBoxSettings.Controls.Add(buttonEditCookie);

            // Totals Group
            GroupBox groupBoxTotals = new GroupBox
            {
                Text = "Totals",
                Location = new Point(400, 360),
                Size = new Size(370, 180),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            this.Controls.Add(groupBoxTotals);

            Label labelGBBought = new Label
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

            Label labelCumulativePoints = new Label
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

            Label labelNextRunIn = new Label
            {
                Text = "Next Run In:",
                Location = new Point(10, 80),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunIn);

            labelNextRunCountdown = new Label
            {
                Text = "",
                Location = new Point(180, 80),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunCountdown);

            // Bottom buttons
            buttonRun = new Button
            {
                Text = "Run Script",
                Location = new Point(10, this.ClientSize.Height - 50),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonRun.Click += ButtonRun_Click;
            this.Controls.Add(buttonRun);

            buttonPause = new Button
            {
                Text = "Pause",
                Location = new Point(120, this.ClientSize.Height - 50),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonPause.Click += ButtonPause_Click;
            this.Controls.Add(buttonPause);

            buttonExit = new Button
            {
                Text = "Exit",
                Location = new Point(230, this.ClientSize.Height - 50),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonExit.Click += ButtonExit_Click;
            this.Controls.Add(buttonExit);

            buttonHelpCookie = new Button
            {
                Text = "Instructions",
                Location = new Point(340, this.ClientSize.Height - 50),
                Size = new Size(150, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonHelpCookie.Click += ButtonHelpCookie_Click;
            this.Controls.Add(buttonHelpCookie);

            buttonCreateCookie = new Button
            {
                Text = "Create my Cookie!",
                Location = new Point(500, this.ClientSize.Height - 50),
                Size = new Size(150, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonCreateCookie.Click += ButtonCreateCookie_Click;
            this.Controls.Add(buttonCreateCookie);

            timerCountdown = new System.Windows.Forms.Timer { Interval = 1000 };
            timerCountdown.Tick += TimerCountdown_Tick;
            timerCountdown.Start();
        }

        private void ButtonBrowseCookie_Click(object? sender, EventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBoxCookieFile.Text = ofd.FileName;
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
            string instructions =
                "MAM Website Instructions:\n" +
                "1. Log in to your Myanonamouse account and go to Preferences => Security.\n" +
                "2. Enter your public IP address (the one Myanonamouse sees) and click 'Submit Changes'.\n" +
                "3. Copy the long security string that is displayed.\n" +
                "4. If your IP is likely to change, click on 'Switch to ASN locked session'.\n\n" +
                "Program Instructions:\n" +
                "1. Use 'Create my Cookie!' to save your security string as a cookie file (you only need to do this once, unless your IP changes).\n" +
                "2. Ensure your cookie file path is entered in the Cookies File field.\n" +
                "3. Adjust your Points Buffer and Next Run Delay as desired.\n" +
                "4. Click 'Run Script' to start the automation.";
            MessageBox.Show(instructions, "Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonCreateCookie_Click(object? sender, EventArgs e)
        {
            string uniqueId = Interaction.InputBox("Enter the unique security string from your account (You only need to do this once! Unless your IP changes):", "Create Cookie File!", "");
            if (!string.IsNullOrEmpty(uniqueId))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*";
                sfd.Title = "Save Cookie File";
                sfd.FileName = "MAM.cookies";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, uniqueId);
                    textBoxCookieFile.Text = sfd.FileName;
                    MessageBox.Show("Cookie file created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ButtonRun_Click(object? sender, EventArgs e)
        {
            if (paused)
            {
                paused = false;
                buttonPause.Text = "Pause";
                AppendLog("Auto-resuming script before running.");
            }
            if (!int.TryParse(textBoxPointsBuffer.Text, out int pointsBuffer))
            {
                MessageBox.Show("Please enter a valid value, 10k recommended.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool vipEnabled = checkBoxBuyVip.Checked;
            if (!int.TryParse(textBoxNextRun.Text, out int nextRunHours))
            {
                MessageBox.Show("Please enter a valid Next Run Delay in hours.", "Invalid Next Run Delay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string cookieFile = textBoxCookieFile.Text;
            Task.Run(() => MainLogic(cookieFile, pointsBuffer, vipEnabled, nextRunHours));
        }

        private void ButtonPause_Click(object? sender, EventArgs e)
        {
            paused = !paused;
            buttonPause.Text = paused ? "Resume" : "Pause";
            if (paused)
            {
                AppendLog("Script paused.");
                labelNextRunCountdown.Text = "Paused";
            }
            else
            {
                AppendLog("Script resumed.");
            }
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
                    if (!int.TryParse(textBoxPointsBuffer.Text, out int pointsBuffer))
                    {
                        AppendLog("Please enter a valid value, 10k recommended.");
                        return;
                    }
                    bool vipEnabled = checkBoxBuyVip.Checked;
                    if (!int.TryParse(textBoxNextRun.Text, out int nextRunHours))
                    {
                        AppendLog("Please enter a valid Next Run Delay in hours.");
                        return;
                    }
                    string cookieFile = textBoxCookieFile.Text;
                    Task.Run(() => MainLogic(cookieFile, pointsBuffer, vipEnabled, nextRunHours));
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

        private void UpdateTotals(int gbBought, int _)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, int>(UpdateTotals), gbBought, 0);
                return;
            }
            labelTotalGB.Text = gbBought.ToString();
        }

        private void SetNextRun(DateTime nextRun)
        {
            nextRunTime = nextRun;
        }

        private HttpClient CreateHttpClient(Dictionary<string, string> cookies)
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri("https://www.myanonamouse.net"), new Cookie("mam_id", cookies["mam_id"]));
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "python-requests/2.28.2");
            return client;
        }

        private async Task<string> GetSessionId(Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                string requestUrl = MAM_API_ENDPOINT + "?snatch_summary";
                var response = await client.GetAsync(requestUrl);
                string responseContent = await response.Content.ReadAsStringAsync();
                AppendLog("Fetched user summary.");
                response.EnsureSuccessStatusCode();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("uid", out JsonElement uidProp))
                    {
                        return uidProp.ValueKind == JsonValueKind.Number ? uidProp.GetInt64().ToString() : uidProp.GetString() ?? "";
                    }
                }
                return "";
            }
        }

        private async Task<Dictionary<string, JsonElement>> GetUserSummary(Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                string requestUrl = MAM_API_ENDPOINT + "?snatch_summary";
                var response = await client.GetAsync(requestUrl);
                string content = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    var dict = new Dictionary<string, JsonElement>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.Clone();
                    }
                    return dict;
                }
            }
        }

        private void UpdateUserInfo(Dictionary<string, JsonElement> summary)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Dictionary<string, JsonElement>>(UpdateUserInfo), summary);
                return;
            }
            labelUserName.Text = summary.TryGetValue("username", out JsonElement userElem) ? userElem.GetString() ?? "N/A" : "N/A";

            if (summary.TryGetValue("vip_until", out JsonElement vipElem))
            {
                string vipStr = vipElem.GetString() ?? "";
                labelVipExpires.Text = DateTime.TryParse(vipStr, out DateTime vipDate) ? vipDate.ToString("MMM dd, yyyy h:mm tt") : vipStr;
            }
            else
            {
                labelVipExpires.Text = "N/A";
            }

            labelDownloaded.Text = summary.TryGetValue("downloaded", out JsonElement dlElem) ? dlElem.GetString() ?? "N/A" : "N/A";
            labelUploaded.Text = summary.TryGetValue("uploaded", out JsonElement ulElem) ? ulElem.GetString() ?? "N/A" : "N/A";
            labelRatio.Text = summary.TryGetValue("ratio", out JsonElement ratioElem) ? ratioElem.ToString() : "N/A";
        }

        private async Task<int> GetSeedBonus(Dictionary<string, string> cookies, string mam_uid)
        {
            using (var client = CreateHttpClient(cookies))
            {
                string url = "https://www.myanonamouse.net/jsonLoad.php?id=" + mam_uid;
                var response = await client.GetAsync(url);
                string responseContent = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("seedbonus", out JsonElement sbProp) &&
                        sbProp.TryGetInt32(out int seedBonus))
                    {
                        return seedBonus;
                    }
                }
                return 0;
            }
        }

        private async Task<DateTime> GetVipExpiry(Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                var response = await client.GetAsync(MAM_API_ENDPOINT);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    string vipUntil = "1970-01-01 00:00:00";
                    if (doc.RootElement.TryGetProperty("vip_until", out JsonElement vipProp))
                    {
                        vipUntil = vipProp.GetString() ?? "1970-01-01 00:00:00";
                    }
                    return DateTime.TryParse(vipUntil, out DateTime vipExpiry) ? vipExpiry : new DateTime(1970, 1, 1);
                }
            }
        }

        private async Task<Dictionary<string, JsonElement>> SendCurlRequest(string url, Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                try
                {
                    var response = await client.GetAsync(url);
                    string json = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        AppendLog($"Error: Status code {response.StatusCode}");
                        throw new Exception($"HTTP request failed with status code {response.StatusCode}: {json}");
                    }
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var dict = new Dictionary<string, JsonElement>();
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value.Clone();
                        }
                        return dict;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("Error sending request to " + url + ": " + ex.Message);
                    throw;
                }
            }
        }

        private async Task MainLogic(string cookieFile, int pointsBuffer, bool vipEnabled, int nextRunHours)
        {
            if (automationRunning)
            {
                AppendLog("Automation already in progress; skipping new run.");
                return;
            }
            automationRunning = true;
            bool vipBought = false;
            int totalUploadGB = 0;

            try
            {
                AppendLog("Starting MAM automation script.");
                var cookies = await LoadCookies(cookieFile);
                SetNextRun(DateTime.Now.AddHours(nextRunHours));
                var summary = await GetUserSummary(cookies);
                UpdateUserInfo(summary);
                string mam_uid = await GetSessionId(cookies);
                if (string.IsNullOrEmpty(mam_uid))
                {
                    AppendLog("Session invalid. Please check your cookie file.");
                    return;
                }
                AppendLog("Existing session valid.");
                AppendLog("Collecting current points.");
                int points = await GetSeedBonus(cookies, mam_uid);
                if (points == 0)
                {
                    AppendLog("Failed to get number of bonus points - aborting.");
                    return;
                }
                AppendLog($"Current points: {points}");

                if (vipEnabled)
                {
                    DateTime vipExpiry = await GetVipExpiry(cookies);
                    TimeSpan vipRemaining = vipExpiry - DateTime.Now;
                    AppendLog($"Current VIP expiry: {vipExpiry:MMM dd, yyyy h:mm tt} ({vipRemaining.TotalDays:F1} days remaining)");
                    if (vipRemaining.TotalDays > 83)
                    {
                        AppendLog("VIP purchase not required; current VIP period exceeds threshold (83 days).");
                    }
                    else
                    {
                        string timestamp = ((long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
                        string vipUrl = VIP_URL_TEMPLATE.Replace("{timestamp}", timestamp);
                        var vipResult = await SendCurlRequest(vipUrl, cookies);
                        if (vipResult.ContainsKey("success") && vipResult["success"].ValueKind == JsonValueKind.True)
                        {
                            AppendLog("VIP purchase successful!");
                            vipBought = true;
                        }
                        else
                        {
                            AppendLog("VIP purchase failed or not available.");
                        }
                    }
                }

                int[] gbValues = new int[] { 100, 20, 5, 1 };
                foreach (int gb in gbValues)
                {
                    int upload_required = gb * 500 + pointsBuffer;
                    while (points > upload_required)
                    {
                        AppendLog($"{points} > {upload_required} - purchasing {gb} GB of upload");
                        string timestamp = ((long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
                        string url = POINTS_URL + gb.ToString() + "&_=" + timestamp;
                        await SendCurlRequest(url, cookies);
                        await Task.Delay(1000);
                        int new_points = await GetSeedBonus(cookies, mam_uid);
                        AppendLog($"After purchase, points: {new_points}");
                        if (new_points < points)
                        {
                            points = new_points;
                            totalUploadGB += gb;
                        }
                        else
                        {
                            AppendLog("Purchase did not reduce points - aborting.");
                            return;
                        }
                    }
                }

                AppendLog("=== Summary ===");
                AppendLog($"VIP Purchase: {(vipBought ? "Yes" : "No")}");
                AppendLog($"Total Upload GB Purchased: {totalUploadGB} GB");
            }
            catch (Exception ex)
            {
                AppendLog("An unexpected error occurred: " + ex.Message);
            }
            finally
            {
                automationRunning = false;
            }
        }

        private Task<Dictionary<string, string>> LoadCookies(string filePath)
        {
            return Task.Run(() =>
            {
                var dict = new Dictionary<string, string>();
                if (!File.Exists(filePath))
                {
                    AppendLog("Cookies file not found. Creating a new one at " + filePath);
                    try
                    {
                        File.WriteAllText(filePath, "");
                    }
                    catch (Exception ex)
                    {
                        AppendLog("Failed to create cookies file: " + ex.Message);
                        throw;
                    }
                    AppendLog("Cookies file created. Please update it with your session cookie.");
                    throw new Exception("Cookies file created. Please update it.");
                }
                try
                {
                    string cookieValue = File.ReadAllText(filePath).Trim();
                    dict["mam_id"] = cookieValue;
                    return dict;
                }
                catch (Exception ex)
                {
                    AppendLog("Error reading cookies file: " + ex.Message);
                    throw;
                }
            });
        }
    }
}
