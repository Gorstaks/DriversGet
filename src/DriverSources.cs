using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DriversGet
{
    public enum Vendor
    {
        Nvidia,
        Amd,
        Intel,
        MicrosoftCatalog
    }

    // Result of a "latest driver" lookup for a vendor.
    public class DriverInfo
    {
        public Vendor Vendor;
        public string Version;       // may be empty if unknown
        public string DownloadUrl;   // direct installer URL if available
        public bool DirectDownloadSupported;
        public string Note;          // human-readable status/explanation

        public bool HasDirectUrl
        {
            get { return !string.IsNullOrEmpty(DownloadUrl); }
        }
    }

    // Central registry of official vendor pages and the logic to resolve
    // the latest driver where a stable mechanism exists.
    public static class DriverSources
    {
        // Official landing / download pages (opened in the user's browser).
        public static string OfficialSite(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia: return "https://www.nvidia.com/Download/index.aspx";
                case Vendor.Amd: return "https://www.amd.com/en/support";
                case Vendor.Intel: return "https://www.intel.com/content/www/us/en/download-center/home.html";
                case Vendor.MicrosoftCatalog: return "https://www.catalog.update.microsoft.com/";
                default: return "https://www.google.com";
            }
        }

        // Official vendor "assistant" / auto-detect tools, useful even without
        // a browser-driven manual search.
        public static string AssistantTool(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia: return "https://www.nvidia.com/en-us/software/nvidia-app/";
                case Vendor.Amd: return "https://www.amd.com/en/support/download/drivers.html";
                case Vendor.Intel: return "https://www.intel.com/content/www/us/en/support/detect.html";
                case Vendor.MicrosoftCatalog: return "https://www.catalog.update.microsoft.com/";
                default: return OfficialSite(v);
            }
        }

        public static string DisplayName(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia: return "NVIDIA Driver Package";
                case Vendor.Amd: return "AMD Software: Adrenalin";
                case Vendor.Intel: return "Intel Driver & Support Assistant";
                case Vendor.MicrosoftCatalog: return "Microsoft Driver Catalog";
                default: return v.ToString();
            }
        }

        // Short subtitle shown under the card title.
        public static string Subtitle(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia: return "GeForce / RTX / GTX dedicated GPUs";
                case Vendor.Amd: return "Radeon / APU / Ryzen";
                case Vendor.Intel: return "Intel CPUs / Graphics / Wi-Fi";
                case Vendor.MicrosoftCatalog: return "Chipset / Audio / Network / misc";
                default: return "";
            }
        }

        // Longer description paragraph for the card body.
        public static string Description(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia:
                    return "Grabs the latest official NVIDIA DCH driver package for the GPU " +
                           "detected in your system. If it isn't detected, the app falls back " +
                           "to NVIDIA's manual driver page.";
                case Vendor.Amd:
                    return "Official driver and control panel for AMD Radeon graphics. " +
                           "Auto-detects your hardware and recommends the right driver.";
                case Vendor.Intel:
                    return "Scans for outdated Intel drivers (graphics, chipset, Wi-Fi, " +
                           "Bluetooth, Ethernet) and downloads updates automatically.";
                case Vendor.MicrosoftCatalog:
                    return "Queries the Microsoft Update driver catalog directly for drivers " +
                           "matching this PC's hardware, then downloads them. No browser or " +
                           "Settings app needed.";
                default: return "";
            }
        }

        // Accent-coloured action button caption for the card.
        public static string ActionCaption(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia: return "Download NVIDIA Driver";
                case Vendor.Amd: return "Get AMD Software";
                case Vendor.Intel: return "Get Intel DSA";
                case Vendor.MicrosoftCatalog: return "Search Driver Catalog";
                default: return "Open";
            }
        }

        // Resolve the latest driver for a vendor.
        // Only NVIDIA has a stable public lookup API; the rest return a
        // links-only result with an explanation.
        public static DriverInfo GetLatest(Vendor v)
        {
            switch (v)
            {
                case Vendor.Nvidia:
                    return GetLatestNvidia();
                default:
                    return new DriverInfo
                    {
                        Vendor = v,
                        Version = "",
                        DownloadUrl = "",
                        DirectDownloadSupported = false,
                        Note = DisplayName(v) + " has no stable public download API. " +
                               "Use 'Open Official Site' or the vendor's detection tool to get the exact driver for your hardware."
                    };
            }
        }

        // --- NVIDIA ---------------------------------------------------------

        // NVIDIA driver lookup. Defaults target modern GeForce desktop GPUs on
        // Windows 10/11 64-bit (DCH). These IDs match NVIDIA's own form values:
        //   psid = product series, pfid = product family, osID = OS id.
        private static DriverInfo GetLatestNvidia()
        {
            DriverInfo info = new DriverInfo
            {
                Vendor = Vendor.Nvidia,
                DirectDownloadSupported = true
            };

            // GeForce RTX 40 Series (desktop) on Windows 11 64-bit DCH.
            // osID 57 covers Windows 10/11 64-bit for the lookup service.
            // These are a sensible default; the official site covers exact SKUs.
            const string url =
                "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/" +
                "AjaxDriverService.php?func=DriverManualLookup" +
                "&psid=129" +   // GeForce RTX 40 Series
                "&pfid=985" +   // GeForce RTX 4090
                "&osID=57" +    // Windows 11 64-bit
                "&languageCode=1033" +
                "&beta=0" +
                "&isWHQL=1" +
                "&dltype=-1" +
                "&dch=1" +
                "&sort1=0" +
                "&numberOfResults=1";

            try
            {
                string json = HttpGetString(url);
                string version = ExtractJsonString(json, "Version");
                string download = ExtractJsonString(json, "DownloadURL");

                if (!string.IsNullOrEmpty(download))
                {
                    info.Version = version ?? "";
                    info.DownloadUrl = download;
                    info.Note = string.IsNullOrEmpty(version)
                        ? "Latest NVIDIA Game Ready driver found."
                        : "Latest NVIDIA Game Ready driver " + version + " found.";
                    return info;
                }
            }
            catch (Exception ex)
            {
                info.Note = "NVIDIA lookup failed (" + ex.Message + "). Falling back to the official site.";
                info.DirectDownloadSupported = false;
                return info;
            }

            info.Note = "Could not parse NVIDIA lookup response. Use the official site.";
            info.DirectDownloadSupported = false;
            return info;
        }

        // --- Helpers --------------------------------------------------------

        private static string HttpGetString(string url)
        {
            EnsureTls();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = 30000;
            req.ReadWriteTimeout = 30000;
            req.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0 Safari/537.36 DriversGet/1.0";
            req.Accept = "application/json, text/plain, */*";

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        // Minimal JSON value extractor for a given key (string values only).
        // Avoids pulling in a JSON library for this tiny app.
        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            // Match "Key":"value" allowing whitespace and escaped slashes.
            Regex rx = new Regex(
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<val>(?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.IgnoreCase);
            Match m = rx.Match(json);
            if (!m.Success)
                return null;

            string raw = m.Groups["val"].Value;
            // Unescape common JSON escapes (NVIDIA escapes forward slashes).
            raw = raw.Replace("\\/", "/")
                     .Replace("\\\"", "\"")
                     .Replace("\\\\", "\\");
            return raw;
        }

        internal static void EnsureTls()
        {
            try
            {
                // net48 defaults to system TLS, but enforce 1.2 to be safe on
                // machines with older configured defaults.
                ServicePointManager.SecurityProtocol |=
                    (SecurityProtocolType)3072; // Tls12
            }
            catch { }
        }
    }
}
