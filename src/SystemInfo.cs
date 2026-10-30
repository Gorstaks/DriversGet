using System;
using System.Collections.Generic;
using System.Management;

namespace DriversGet
{
    // Lightweight hardware/OS detection used to pick smarter defaults
    // (e.g. detect an NVIDIA GPU so we can offer the right driver).
    public static class SystemInfo
    {
        public static bool Is64BitOs
        {
            get { return Environment.Is64BitOperatingSystem; }
        }

        public static string OsCaption()
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in s.Get())
                    {
                        object cap = mo["Caption"];
                        if (cap != null)
                            return cap.ToString().Trim();
                    }
                }
            }
            catch { }
            return Environment.OSVersion.VersionString;
        }

        // Returns the display adapter names reported by Windows.
        public static List<string> VideoControllers()
        {
            List<string> list = new List<string>();
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject mo in s.Get())
                    {
                        object name = mo["Name"];
                        if (name != null)
                            list.Add(name.ToString().Trim());
                    }
                }
            }
            catch { }
            return list;
        }

        public static bool HasNvidiaGpu()
        {
            return ContainsVendor("nvidia") || ContainsVendor("geforce") || ContainsVendor("quadro") || ContainsVendor("rtx") || ContainsVendor("gtx");
        }

        public static bool HasAmdGpu()
        {
            return ContainsVendor("amd") || ContainsVendor("radeon") || ContainsVendor("ati");
        }

        public static bool HasIntelGpu()
        {
            return ContainsVendor("intel");
        }

        private static bool ContainsVendor(string needle)
        {
            foreach (string name in VideoControllers())
            {
                if (name != null && name.ToLowerInvariant().Contains(needle))
                    return true;
            }
            return false;
        }
    }
}
