using System;
using System.Drawing;
using System.Windows.Forms;

namespace DriversGet
{
    // Small modal settings dialog styled to match the dark theme.
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private TextBox _folderBox;
        private NumericUpDown _interval;
        private CheckBox _checkStartup;
        private CheckBox _startMin;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "DriversGet - Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 240);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Font(9.5f);

            Label folderLabel = NewLabel("Download folder:", 16, 18);
            Controls.Add(folderLabel);

            _folderBox = new TextBox();
            Theme.StyleTextBox(_folderBox);
            _folderBox.Text = _settings.DownloadFolder;
            _folderBox.Location = new Point(16, 40);
            _folderBox.Width = 420;
            Controls.Add(_folderBox);

            Button browse = Theme.MakeButton("Browse...", false);
            browse.Location = new Point(446, 38);
            browse.Width = 96;
            browse.Click += delegate
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.SelectedPath = _folderBox.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        _folderBox.Text = dlg.SelectedPath;
                }
            };
            Controls.Add(browse);

            Label intervalLabel = NewLabel("Check interval (hours):", 16, 84);
            Controls.Add(intervalLabel);

            _interval = new NumericUpDown();
            _interval.Minimum = 1;
            _interval.Maximum = 24 * 14;
            _interval.Value = Math.Max(1, Math.Min(24 * 14, _settings.CheckIntervalHours));
            _interval.Location = new Point(180, 82);
            _interval.Width = 80;
            _interval.BackColor = Theme.Input;
            _interval.ForeColor = Theme.TextBright;
            _interval.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_interval);

            _checkStartup = NewCheck("Check for updates on startup", 16, 118, _settings.CheckOnStartup);
            Controls.Add(_checkStartup);

            _startMin = NewCheck("Start minimized to tray", 16, 144, _settings.StartMinimized);
            Controls.Add(_startMin);

            Button ok = Theme.MakeButton("Save", true);
            ok.Location = new Point(336, 192);
            ok.Width = 100;
            ok.Click += delegate
            {
                _settings.DownloadFolder = _folderBox.Text.Trim();
                _settings.CheckIntervalHours = (int)_interval.Value;
                _settings.CheckOnStartup = _checkStartup.Checked;
                _settings.StartMinimized = _startMin.Checked;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            Button cancel = Theme.MakeButton("Cancel", false);
            cancel.Location = new Point(446, 192);
            cancel.Width = 96;
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private Label NewLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = Theme.Text;
            l.Font = Theme.Font(9.5f);
            l.AutoSize = true;
            l.Location = new Point(x, y);
            return l;
        }

        private CheckBox NewCheck(string text, int x, int y, bool chk)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.ForeColor = Theme.Text;
            c.Font = Theme.Font(9.5f);
            c.AutoSize = true;
            c.Checked = chk;
            c.Location = new Point(x, y);
            return c;
        }
    }
}
