using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace DriversGet
{
    public class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly Downloader _downloader = new Downloader();

        private TableLayoutPanel _cardGrid;
        private TextBox _log;
        private ProgressBar _progress;
        private Label _progressLabel;
        private Button _cancelButton;

        private Label _nvidiaStatus;

        // When true, the form closes for real instead of hiding to tray.
        public bool AllowRealClose { get; set; }

        public MainForm(AppSettings settings)
        {
            _settings = settings;
            BuildUi();
            WireDownloader();
            Log("DriversGet started. OS: " + SystemInfo.OsCaption());
            Log("Detected GPUs: " + string.Join(", ", SystemInfo.VideoControllers().ToArray()));
        }

        private void BuildUi()
        {
            Text = "DriversGet - Driver Tools";
            Width = 920;
            Height = 760;
            MinimumSize = new Size(820, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Font(9.5f);

            // Root layout: header / cards / status strip / progress / log.
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.BackColor = Theme.Background;
            root.Padding = new Padding(0);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // cards
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // footer note + settings
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // progress
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));  // log
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildCardsArea(), 0, 1);
            root.Controls.Add(BuildFooterStrip(), 0, 2);
            root.Controls.Add(BuildProgressArea(), 0, 3);
            root.Controls.Add(BuildLogArea(), 0, 4);
        }

        private Control BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Theme.Panel;
            header.Padding = new Padding(20, 12, 20, 12);

            Label title = new Label();
            title.Text = "DriversGet";
            title.Font = Theme.Font(15f, FontStyle.Bold);
            title.ForeColor = Theme.Accent;
            title.AutoSize = true;
            title.Location = new Point(20, 12);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Official driver updates - grab the latest from your hardware maker";
            subtitle.Font = Theme.Font(9f);
            subtitle.ForeColor = Theme.TextMuted;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(22, 42);
            header.Controls.Add(subtitle);

            Button scanBtn = Theme.MakeButton("Check for Updates", true);
            scanBtn.Width = 160;
            scanBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            scanBtn.Location = new Point(header.Width - 180, 18);
            scanBtn.Click += delegate { CheckNvidiaForUpdates(true, null); };
            header.Controls.Add(scanBtn);
            header.Resize += delegate { scanBtn.Location = new Point(header.Width - 180, 18); };

            return header;
        }

        private Control BuildCardsArea()
        {
            Panel wrap = new Panel();
            wrap.Dock = DockStyle.Fill;
            wrap.BackColor = Theme.Background;
            wrap.Padding = new Padding(20, 16, 20, 8);
            wrap.AutoScroll = true;

            _cardGrid = new TableLayoutPanel();
            _cardGrid.Dock = DockStyle.Fill;
            _cardGrid.ColumnCount = 2;
            _cardGrid.RowCount = 2;
            _cardGrid.BackColor = Theme.Background;
            for (int c = 0; c < 2; c++)
                _cardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int r = 0; r < 2; r++)
                _cardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            _cardGrid.Controls.Add(BuildCard(Vendor.Nvidia, Theme.Green, out _nvidiaStatus), 0, 0);
            Label ignore;
            _cardGrid.Controls.Add(BuildCard(Vendor.Amd, Theme.Danger, out ignore), 1, 0);
            _cardGrid.Controls.Add(BuildCard(Vendor.Intel, Theme.Accent, out ignore), 0, 1);
            _cardGrid.Controls.Add(BuildCard(Vendor.MicrosoftCatalog, Theme.Accent, out ignore), 1, 1);

            wrap.Controls.Add(_cardGrid);
            return wrap;
        }

        private Control BuildCard(Vendor vendor, Color iconColor, out Label status)
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(8);
            card.Padding = new Padding(18, 16, 18, 16);

            // Controls are docked so they never fall off the bottom of a short
            // card. Dock order matters: last-added fills the remaining space,
            // so add the bottom items first, then the description fills.

            // 1) Header row: coloured icon chip + title/subtitle (docked top).
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 44;
            header.BackColor = Theme.Panel;

            Panel iconChip = new Panel();
            iconChip.Size = new Size(34, 34);
            iconChip.Location = new Point(0, 4);
            iconChip.BackColor = iconColor;
            header.Controls.Add(iconChip);

            Label title = new Label();
            title.Text = DriverSources.DisplayName(vendor);
            title.Font = Theme.Font(10.5f, FontStyle.Bold);
            title.ForeColor = Theme.TextBright;
            title.AutoSize = true;
            title.Location = new Point(44, 2);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = DriverSources.Subtitle(vendor);
            subtitle.Font = Theme.Font(8f);
            subtitle.ForeColor = Theme.TextDim;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(44, 23);
            header.Controls.Add(subtitle);

            // 2) Action button (docked bottom).
            Button action = Theme.MakeButton(DriverSources.ActionCaption(vendor), true);
            action.Dock = DockStyle.Bottom;
            action.Height = 34;
            Vendor vCopy = vendor;

            // 3) Status line (docked bottom, above button).
            status = new Label();
            status.Text = "";
            status.Font = Theme.Font(8f, FontStyle.Italic);
            status.ForeColor = Theme.Green;
            status.AutoSize = false;
            status.Dock = DockStyle.Bottom;
            status.Height = 18;
            status.TextAlign = ContentAlignment.MiddleLeft;

            // 4) Description fills whatever is left between header and status.
            Label desc = new Label();
            desc.Text = DriverSources.Description(vendor);
            desc.Font = Theme.Font(8.75f);
            desc.ForeColor = Theme.TextMuted;
            desc.Dock = DockStyle.Fill;
            desc.AutoSize = false;
            desc.Padding = new Padding(0, 6, 0, 6);

            Label statusCopy = status;
            action.Click += delegate { OnVendorAction(vCopy, statusCopy); };

            // Add in the order that produces the correct dock stacking:
            // Fill first (so it occupies center), then the docked edges.
            // For two Bottom-docked controls, the one added FIRST sits at the
            // outer (lowest) edge. Add the button first so it's at the very
            // bottom, then status docks just above it, then desc fills, header
            // tops it off.
            card.Controls.Clear();
            card.Controls.Add(action);   // Bottom (lowest)
            card.Controls.Add(status);   // Bottom (above button)
            card.Controls.Add(desc);     // Fill (center)
            card.Controls.Add(header);   // Top

            return card;
        }

        private Control BuildFooterStrip()
        {
            Panel strip = new Panel();
            strip.Dock = DockStyle.Fill;
            strip.BackColor = Theme.Background;
            strip.Padding = new Padding(20, 2, 20, 2);

            Label note = new Label();
            note.Text = "For Realtek and other component-level drivers, check your laptop or motherboard manufacturer's support site.";
            note.Font = Theme.Font(8.5f);
            note.ForeColor = Theme.TextDim;
            note.AutoSize = false;
            note.Dock = DockStyle.Left;
            note.Width = 560;
            note.TextAlign = ContentAlignment.MiddleLeft;
            strip.Controls.Add(note);

            Button linksBtn = Theme.MakeButton("Official Links...", false);
            linksBtn.Width = 130;
            linksBtn.Dock = DockStyle.Right;
            linksBtn.Click += delegate { ShowLinksMenu(linksBtn); };
            strip.Controls.Add(linksBtn);

            Button settingsBtn = Theme.MakeButton("Settings...", false);
            settingsBtn.Width = 110;
            settingsBtn.Dock = DockStyle.Right;
            settingsBtn.Margin = new Padding(0, 0, 8, 0);
            settingsBtn.Click += delegate { ShowSettingsDialog(); };
            strip.Controls.Add(settingsBtn);

            return strip;
        }

        private Control BuildProgressArea()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Theme.Background;
            panel.Padding = new Padding(20, 6, 20, 6);

            _progressLabel = new Label();
            _progressLabel.Text = "Idle.";
            _progressLabel.ForeColor = Theme.TextMuted;
            _progressLabel.Font = Theme.Font(8.5f);
            _progressLabel.Dock = DockStyle.Top;
            _progressLabel.Height = 16;
            panel.Controls.Add(_progressLabel);

            _cancelButton = Theme.MakeButton("Cancel", false);
            _cancelButton.Width = 90;
            _cancelButton.Height = 24;
            _cancelButton.Dock = DockStyle.Right;
            _cancelButton.Enabled = false;
            _cancelButton.Click += delegate { _downloader.Cancel(); };
            panel.Controls.Add(_cancelButton);

            _progress = new ProgressBar();
            _progress.Dock = DockStyle.Fill;
            _progress.Height = 10;
            _progress.ForeColor = Theme.Accent;
            _progress.BackColor = Theme.ChromeBg;
            panel.Controls.Add(_progress);

            _progress.BringToFront();
            return panel;
        }

        private Control BuildLogArea()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Theme.Background;
            panel.Padding = new Padding(20, 0, 20, 12);

            _log = new TextBox();
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.Dock = DockStyle.Fill;
            _log.BackColor = Theme.Card;
            _log.ForeColor = Theme.Text;
            _log.BorderStyle = BorderStyle.FixedSingle;
            _log.Font = new Font("Consolas", 8.5f);
            panel.Controls.Add(_log);

            return panel;
        }

        private void ShowLinksMenu(Control anchor)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Theme.Panel;
            menu.ForeColor = Theme.Text;

            AddLinkItem(menu, "NVIDIA Drivers", DriverSources.OfficialSite(Vendor.Nvidia));
            AddLinkItem(menu, "NVIDIA App (official updater)", DriverSources.AssistantTool(Vendor.Nvidia));
            AddLinkItem(menu, "AMD Support & Drivers", DriverSources.OfficialSite(Vendor.Amd));
            AddLinkItem(menu, "AMD Auto-Detect Tool", DriverSources.AssistantTool(Vendor.Amd));
            AddLinkItem(menu, "Intel Download Center", DriverSources.OfficialSite(Vendor.Intel));
            AddLinkItem(menu, "Intel Driver & Support Assistant", DriverSources.AssistantTool(Vendor.Intel));
            AddLinkItem(menu, "Microsoft Update Catalog (web)", DriverSources.OfficialSite(Vendor.MicrosoftCatalog));

            menu.Show(anchor, new Point(0, -menu.Items.Count * 24));
        }

        private void AddLinkItem(ContextMenuStrip menu, string text, string url)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.ForeColor = Theme.Text;
            string u = url;
            item.Click += delegate { OpenUrl(u); };
            menu.Items.Add(item);
        }

        private void ShowSettingsDialog()
        {
            using (SettingsForm dlg = new SettingsForm(_settings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.Save();
                    Log("Settings saved.");
                    if (SettingsChanged != null)
                        SettingsChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler SettingsChanged;

        // Allows the tray context menu to open the settings dialog.
        public void OpenSettingsExternally()
        {
            ShowSettingsDialog();
        }

        private void WireDownloader()
        {
            _downloader.Progress += delegate(object s, DownloadProgressEventArgs e)
            {
                RunOnUi(delegate
                {
                    _progress.Value = Math.Max(0, Math.Min(100, e.Percent));
                    _progressLabel.Text = string.Format(
                        "Downloading... {0}% ({1:n0} / {2:n0} bytes)",
                        e.Percent, e.BytesReceived, e.TotalBytes);
                });
            };

            _downloader.Completed += delegate(object s, DownloadCompletedEventArgs e)
            {
                RunOnUi(delegate
                {
                    _cancelButton.Enabled = false;
                    if (e.Success)
                    {
                        _progress.Value = 100;
                        _progressLabel.Text = "Download complete.";
                        Log("Saved: " + e.FilePath);
                        if (MessageBox.Show(this,
                                "Driver downloaded to:\n" + e.FilePath +
                                "\n\nOpen the containing folder now?",
                                "DriversGet", MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            OpenFolder(e.FilePath);
                        }
                    }
                    else
                    {
                        _progress.Value = 0;
                        _progressLabel.Text = "Download failed.";
                        Log("Download failed: " + e.Error);
                    }
                });
            };
        }

        private void OnVendorAction(Vendor vendor, Label status)
        {
            // The Microsoft catalog opens a search dialog driven by the Windows
            // Update Agent (no browser / Settings app).
            if (vendor == Vendor.MicrosoftCatalog)
            {
                Log("Opening Microsoft Driver Catalog search...");
                using (CatalogForm dlg = new CatalogForm())
                {
                    dlg.ShowDialog(this);
                }
                return;
            }

            Log("Looking up latest " + DriverSources.DisplayName(vendor) + "...");
            if (status != null) status.Text = "Checking...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                DriverInfo info = DriverSources.GetLatest(vendor);
                RunOnUi(delegate { HandleLookupResult(info, status); });
            });
        }

        private void HandleLookupResult(DriverInfo info, Label status)
        {
            Log(DriverSources.DisplayName(info.Vendor) + ": " + info.Note);

            if (info.HasDirectUrl)
            {
                if (status != null)
                    status.Text = string.IsNullOrEmpty(info.Version)
                        ? "Latest driver available."
                        : "Latest: " + info.Version;

                if (_downloader.IsBusy)
                {
                    Log("A download is already in progress. Please wait.");
                    return;
                }

                _progress.Value = 0;
                _progressLabel.Text = "Starting download...";
                _cancelButton.Enabled = true;
                _downloader.Start(info.DownloadUrl, _settings.DownloadFolder, null);
                Log("Downloading from: " + info.DownloadUrl);
            }
            else
            {
                if (status != null) status.Text = "Opening official site...";
                OpenUrl(DriverSources.OfficialSite(info.Vendor));
            }
        }

        // Called by the tray/periodic check to look up NVIDIA's latest driver.
        public void CheckNvidiaForUpdates(bool interactive, Action<DriverInfo> onResult)
        {
            Log("Checking NVIDIA for the latest driver...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                DriverInfo info = DriverSources.GetLatest(Vendor.Nvidia);
                RunOnUi(delegate
                {
                    Log("Check result: " + info.Note);
                    if (_nvidiaStatus != null && !string.IsNullOrEmpty(info.Version))
                        _nvidiaStatus.Text = "Latest: " + info.Version;
                    if (onResult != null)
                        onResult(info);
                });
            });
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
                Log("Opened: " + url);
            }
            catch (Exception ex)
            {
                Log("Could not open (" + ex.Message + "): " + url);
                MessageBox.Show(this,
                    "Could not open this target. The link is:\n\n" + url,
                    "DriversGet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFolder(string filePath)
        {
            try
            {
                Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
            }
            catch (Exception ex)
            {
                Log("Could not open folder: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            RunOnUi(delegate
            {
                _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
            });
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(action); }
                catch { }
            }
            else
            {
                action();
            }
        }
    }
}
