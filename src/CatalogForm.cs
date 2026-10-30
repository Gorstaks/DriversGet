using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace DriversGet
{
    // Shows driver updates found via the Windows Update Agent and lets the user
    // download a selected one into the WUA cache.
    public class CatalogForm : Form
    {
        private ListBox _list;
        private Button _searchBtn;
        private Button _downloadBtn;
        private Button _webBtn;
        private Label _statusLabel;
        private CatalogResult _lastResult;

        public CatalogForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "DriversGet - Microsoft Driver Catalog";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 460);
            MinimumSize = new Size(560, 360);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Font(9.5f);

            Label heading = new Label();
            heading.Text = "Driver updates from the Microsoft Update catalog";
            heading.Font = Theme.Font(11f, FontStyle.Bold);
            heading.ForeColor = Theme.TextBright;
            heading.AutoSize = true;
            heading.Location = new Point(16, 14);
            Controls.Add(heading);

            Label sub = new Label();
            sub.Text = "Queries the Windows Update Agent directly for drivers matching this PC's hardware. No browser or Settings app involved.";
            sub.Font = Theme.Font(8.5f);
            sub.ForeColor = Theme.TextMuted;
            sub.AutoSize = false;
            sub.Location = new Point(16, 40);
            sub.Size = new Size(688, 18);
            sub.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(sub);

            _list = new ListBox();
            _list.Location = new Point(16, 66);
            _list.Size = new Size(688, 320);
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.BackColor = Theme.Card;
            _list.ForeColor = Theme.Text;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.Font = Theme.Font(9f);
            _list.IntegralHeight = false;
            Controls.Add(_list);

            _statusLabel = new Label();
            _statusLabel.Text = "Click Search to query the catalog.";
            _statusLabel.ForeColor = Theme.TextMuted;
            _statusLabel.Font = Theme.Font(8.5f);
            _statusLabel.AutoSize = false;
            _statusLabel.Location = new Point(16, 398);
            _statusLabel.Size = new Size(270, 48);
            _statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(_statusLabel);

            _searchBtn = Theme.MakeButton("Search", true);
            _searchBtn.Width = 120;
            _searchBtn.Location = new Point(456, 404);
            _searchBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _searchBtn.Click += delegate { StartSearch(); };
            Controls.Add(_searchBtn);

            _downloadBtn = Theme.MakeButton("Download Selected", false);
            _downloadBtn.Width = 140;
            _downloadBtn.Location = new Point(584, 404);
            _downloadBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _downloadBtn.Enabled = false;
            _downloadBtn.Click += delegate { DownloadSelected(); };
            Controls.Add(_downloadBtn);

            _webBtn = Theme.MakeButton("Open Web Catalog", false);
            _webBtn.Width = 150;
            _webBtn.Location = new Point(296, 404);
            _webBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _webBtn.Visible = false;
            _webBtn.Click += delegate
            {
                try { System.Diagnostics.Process.Start(DriverSources.OfficialSite(Vendor.MicrosoftCatalog)); }
                catch { }
            };
            Controls.Add(_webBtn);

            Shown += delegate { StartSearch(); };
        }

        private void StartSearch()
        {
            _searchBtn.Enabled = false;
            _downloadBtn.Enabled = false;
            _list.Items.Clear();
            SetStatus("Searching the Microsoft Update catalog (this can take a minute)...");

            ThreadPool.QueueUserWorkItem(delegate
            {
                CatalogResult result = WindowsDriverCatalog.SearchDrivers();
                RunOnUi(delegate { ShowResults(result); });
            });
        }

        private void ShowResults(CatalogResult result)
        {
            _lastResult = result;
            _searchBtn.Enabled = true;

            if (!result.Success)
            {
                SetStatus("Search failed: " + result.Error);
                _webBtn.Visible = true; // offer the web catalog as a fallback
                return;
            }
            _webBtn.Visible = false;
            if (result.Drivers.Count == 0)
            {
                SetStatus("No applicable driver updates found. Your drivers may already be current.");
                return;
            }

            foreach (CatalogDriver d in result.Drivers)
                _list.Items.Add(d.DisplayLine);

            _downloadBtn.Enabled = true;
            _list.SelectedIndex = 0;
            SetStatus(result.Drivers.Count + " driver update(s) found. Select one and click Download Selected.");
        }

        private void DownloadSelected()
        {
            int idx = _list.SelectedIndex;
            if (_lastResult == null || idx < 0 || idx >= _lastResult.Drivers.Count)
            {
                SetStatus("Select a driver first.");
                return;
            }

            CatalogDriver driver = _lastResult.Drivers[idx];
            _downloadBtn.Enabled = false;
            _searchBtn.Enabled = false;
            SetStatus("Downloading: " + driver.Title + " ...");

            ThreadPool.QueueUserWorkItem(delegate
            {
                string status = WindowsDriverCatalog.DownloadDriver(driver);
                RunOnUi(delegate
                {
                    SetStatus(status);
                    _downloadBtn.Enabled = true;
                    _searchBtn.Enabled = true;
                });
            });
        }

        private void SetStatus(string text)
        {
            _statusLabel.Text = text;
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
