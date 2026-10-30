using System;
using System.IO;
using System.Text;

namespace DriversGet
{
    // Simple settings persisted to a tiny INI-style file in %APPDATA%\DriversGet.
    // Kept dependency-free (no JSON library) to stay a "tiny" app on C# 5 / net48.
    public class AppSettings
    {
        public string DownloadFolder { get; set; }
        public int CheckIntervalHours { get; set; }
        public bool CheckOnStartup { get; set; }
        public bool StartMinimized { get; set; }
        // Last NVIDIA driver version we told the user about (to avoid repeat nagging).
        public string LastSeenNvidiaVersion { get; set; }

        public AppSettings()
        {
            DownloadFolder = DefaultDownloadFolder();
            CheckIntervalHours = 24;
            CheckOnStartup = true;
            StartMinimized = false;
            LastSeenNvidiaVersion = "";
        }

        public static string DefaultDownloadFolder()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(baseDir, Path.Combine("Downloads", "DriversGet"));
        }

        private static string ConfigDir()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DriversGet");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private static string ConfigPath()
        {
            return Path.Combine(ConfigDir(), "settings.ini");
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                string path = ConfigPath();
                if (!File.Exists(path))
                    return s;

                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "DownloadFolder":
                            if (val.Length > 0) s.DownloadFolder = val;
                            break;
                        case "CheckIntervalHours":
                            int hrs;
                            if (int.TryParse(val, out hrs) && hrs > 0) s.CheckIntervalHours = hrs;
                            break;
                        case "CheckOnStartup":
                            s.CheckOnStartup = ParseBool(val, true);
                            break;
                        case "StartMinimized":
                            s.StartMinimized = ParseBool(val, false);
                            break;
                        case "LastSeenNvidiaVersion":
                            s.LastSeenNvidiaVersion = val;
                            break;
                    }
                }
            }
            catch
            {
                // Corrupt or unreadable settings should never crash the app.
            }
            return s;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# DriversGet settings");
                sb.AppendLine("DownloadFolder=" + (DownloadFolder ?? ""));
                sb.AppendLine("CheckIntervalHours=" + CheckIntervalHours);
                sb.AppendLine("CheckOnStartup=" + (CheckOnStartup ? "true" : "false"));
                sb.AppendLine("StartMinimized=" + (StartMinimized ? "true" : "false"));
                sb.AppendLine("LastSeenNvidiaVersion=" + (LastSeenNvidiaVersion ?? ""));
                File.WriteAllText(ConfigPath(), sb.ToString());
            }
            catch
            {
                // Best-effort save.
            }
        }

        private static bool ParseBool(string val, bool fallback)
        {
            if (string.IsNullOrEmpty(val)) return fallback;
            val = val.Trim().ToLowerInvariant();
            return val == "true" || val == "1" || val == "yes";
        }
    }
}
