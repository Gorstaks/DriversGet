using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace DriversGet
{
    internal static class Program
    {
        private static Mutex _singleInstance;

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            _singleInstance = new Mutex(true, "DriversGet_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                // Already running; just exit (the tray icon of the first
                // instance remains available).
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppSettings settings = AppSettings.Load();
            using (TrayAppContext ctx = new TrayAppContext(settings))
            {
                Application.Run(ctx);
            }

            GC.KeepAlive(_singleInstance);
        }
    }

    // Owns the tray icon, the main window, and the periodic check timer.
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly AppSettings _settings;
        private readonly NotifyIcon _tray;
        private readonly System.Windows.Forms.Timer _timer;
        private MainForm _form;

        public TrayAppContext(AppSettings settings)
        {
            _settings = settings;

            _tray = new NotifyIcon();
            _tray.Icon = SystemIcons.Information;
            _tray.Text = "DriversGet";
            _tray.Visible = true;
            _tray.DoubleClick += delegate { ShowWindow(); };
            _tray.ContextMenuStrip = BuildTrayMenu();

            _timer = new System.Windows.Forms.Timer();
            _timer.Tick += delegate { RunPeriodicCheck(); };
            RescheduleTimer();

            EnsureForm();

            if (_settings.StartMinimized)
            {
                _form.WindowState = FormWindowState.Minimized;
                _form.ShowInTaskbar = false;
                _form.Hide();
            }
            else
            {
                ShowWindow();
            }

            if (_settings.CheckOnStartup)
                RunPeriodicCheck();
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open DriversGet", null, delegate { ShowWindow(); });
            menu.Items.Add("Check Now", null, delegate { RunPeriodicCheck(); });
            menu.Items.Add("Settings...", null, delegate { OpenSettings(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitApp(); });
            return menu;
        }

        private void EnsureForm()
        {
            if (_form == null || _form.IsDisposed)
            {
                _form = new MainForm(_settings);
                _form.SettingsChanged += delegate { RescheduleTimer(); };
                // Closing the window hides to tray instead of exiting.
                _form.FormClosing += delegate(object s, FormClosingEventArgs e)
                {
                    if (e.CloseReason == CloseReason.UserClosing && !_form.AllowRealClose)
                    {
                        e.Cancel = true;
                        _form.Hide();
                        _form.ShowInTaskbar = false;
                        _tray.ShowBalloonTip(2000, "DriversGet",
                            "Still running in the tray. Right-click the icon to exit.",
                            ToolTipIcon.Info);
                    }
                };
            }
        }

        private void ShowWindow()
        {
            EnsureForm();
            _form.Show();
            _form.ShowInTaskbar = true;
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
            _form.BringToFront();
        }

        private void OpenSettings()
        {
            EnsureForm();
            ShowWindow();
            // Reuse the form's settings dialog via a public trigger.
            _form.OpenSettingsExternally();
        }

        private void RescheduleTimer()
        {
            int hours = _settings.CheckIntervalHours;
            if (hours < 1) hours = 1;
            // Timer interval is milliseconds; cap to int range safely.
            long ms = (long)hours * 60L * 60L * 1000L;
            if (ms > int.MaxValue) ms = int.MaxValue;
            _timer.Interval = (int)ms;
            _timer.Stop();
            _timer.Start();
        }

        private void RunPeriodicCheck()
        {
            EnsureForm();
            _form.CheckNvidiaForUpdates(false, delegate(DriverInfo info)
            {
                if (info != null && !string.IsNullOrEmpty(info.Version) &&
                    !string.Equals(info.Version, _settings.LastSeenNvidiaVersion, StringComparison.OrdinalIgnoreCase))
                {
                    _settings.LastSeenNvidiaVersion = info.Version;
                    _settings.Save();
                    _tray.ShowBalloonTip(5000, "NVIDIA driver available",
                        "Latest Game Ready driver " + info.Version +
                        " is available. Open DriversGet to download.",
                        ToolTipIcon.Info);
                }
            });
        }

        private void ExitApp()
        {
            _timer.Stop();
            _tray.Visible = false;
            if (_form != null && !_form.IsDisposed)
            {
                _form.AllowRealClose = true;
                _form.Close();
            }
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) _timer.Dispose();
                if (_tray != null) _tray.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
