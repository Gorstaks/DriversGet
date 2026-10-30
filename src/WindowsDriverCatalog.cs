using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DriversGet
{
    // One driver update returned by the Windows Update Agent.
    public class CatalogDriver
    {
        public string Title;
        public string DriverVersion;
        public string DriverModel;
        public string DriverProvider;
        public DateTime DriverVerDate;
        public double SizeMb;
        public object UpdateComObject; // the IUpdate COM object, for download

        public string DisplayLine
        {
            get
            {
                string ver = string.IsNullOrEmpty(DriverVersion) ? "" : " v" + DriverVersion;
                string date = DriverVerDate == DateTime.MinValue ? "" : " (" + DriverVerDate.ToString("yyyy-MM-dd") + ")";
                string size = SizeMb > 0 ? string.Format(" - {0:0.#} MB", SizeMb) : "";
                return Title + ver + date + size;
            }
        }
    }

    public class CatalogResult
    {
        public bool Success;
        public string Error;
        public List<CatalogDriver> Drivers = new List<CatalogDriver>();
    }

    // Queries the Microsoft Update driver catalog directly via the Windows
    // Update Agent (WUA) COM API, using late binding so no COM interop
    // assembly is needed at build time (keeps the csc.exe build dependency-free).
    //
    // This does NOT route through the Settings "Windows Update" page; it drives
    // the update agent itself to search Microsoft's online driver catalog for
    // the local machine's hardware.
    public static class WindowsDriverCatalog
    {
        // Searches the online Microsoft Update service for driver updates that
        // apply to this PC but are not yet installed.
        public static CatalogResult SearchDrivers()
        {
            CatalogResult result = new CatalogResult();
            object session = null;
            try
            {
                Type sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                if (sessionType == null)
                {
                    result.Error = "Windows Update Agent (Microsoft.Update.Session) is not available on this system.";
                    return result;
                }

                session = Activator.CreateInstance(sessionType);

                object searcher = Invoke(session, "CreateUpdateSearcher");

                // Search the online default service (Microsoft Update) rather
                // than only locally cached results.
                SetProp(searcher, "Online", true);

                // Driver updates not yet installed and not hidden.
                const string criteria = "IsInstalled=0 and Type='Driver' and IsHidden=0";

                object searchResult = Invoke(searcher, "Search", criteria);
                object updates = GetProp(searchResult, "Updates");
                int count = (int)GetProp(updates, "Count");

                for (int i = 0; i < count; i++)
                {
                    object update = Invoke(updates, "get_Item", i);
                    CatalogDriver d = ReadDriver(update);
                    if (d != null)
                        result.Drivers.Add(d);
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = Flatten(ex);
                return result;
            }
        }

        // Downloads a single driver update's payload into the WUA cache and
        // returns a human-readable status. (Install needs elevation and is
        // intentionally left to the user, matching the rest of the app.)
        public static string DownloadDriver(CatalogDriver driver)
        {
            if (driver == null || driver.UpdateComObject == null)
                return "No update selected.";

            object session = null;
            try
            {
                Type sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                session = Activator.CreateInstance(sessionType);

                // Build an UpdateCollection containing just this update.
                Type collType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl");
                object coll = Activator.CreateInstance(collType);
                Invoke(coll, "Add", driver.UpdateComObject);

                object downloader = Invoke(session, "CreateUpdateDownloader");
                SetProp(downloader, "Updates", coll);
                Invoke(downloader, "Download");

                bool isDownloaded = (bool)GetProp(driver.UpdateComObject, "IsDownloaded");
                return isDownloaded
                    ? "Downloaded to the Windows Update cache. Install it from Device Manager > Update driver, or let Windows apply it."
                    : "Download finished but the update does not report as downloaded.";
            }
            catch (Exception ex)
            {
                return "Download failed: " + Flatten(ex);
            }
        }

        private static CatalogDriver ReadDriver(object update)
        {
            try
            {
                CatalogDriver d = new CatalogDriver();
                d.UpdateComObject = update;
                d.Title = AsString(GetProp(update, "Title"));

                // Driver-specific fields live on the IWindowsDriverUpdate face,
                // but late-bound property access works on the same object.
                d.DriverVersion = TryStringProp(update, "DriverVerVersion");
                d.DriverModel = TryStringProp(update, "DriverModel");
                d.DriverProvider = TryStringProp(update, "DriverProvider");

                object dt = TryProp(update, "DriverVerDate");
                if (dt is DateTime)
                    d.DriverVerDate = (DateTime)dt;

                object maxSize = TryProp(update, "MaxDownloadSize");
                if (maxSize != null)
                {
                    try
                    {
                        double bytes = Convert.ToDouble(maxSize);
                        d.SizeMb = bytes / (1024.0 * 1024.0);
                    }
                    catch { }
                }

                return d;
            }
            catch
            {
                return null;
            }
        }

        // --- late-binding helpers ------------------------------------------

        private static object Invoke(object target, string name, params object[] args)
        {
            return target.GetType().InvokeMember(
                name, BindingFlags.InvokeMethod, null, target, args);
        }

        private static object GetProp(object target, string name)
        {
            return target.GetType().InvokeMember(
                name, BindingFlags.GetProperty, null, target, null);
        }

        private static void SetProp(object target, string name, object value)
        {
            target.GetType().InvokeMember(
                name, BindingFlags.SetProperty, null, target, new object[] { value });
        }

        private static object TryProp(object target, string name)
        {
            try { return GetProp(target, name); }
            catch { return null; }
        }

        private static string TryStringProp(object target, string name)
        {
            return AsString(TryProp(target, name));
        }

        private static string AsString(object o)
        {
            return o == null ? "" : o.ToString();
        }

        private static string Flatten(Exception ex)
        {
            // Late-bound COM calls wrap the real failure in a
            // TargetInvocationException, so the meaningful HRESULT lives on an
            // inner exception. Walk the whole chain looking for a known WUA code.
            int friendlyHr = 0;
            string friendly = null;
            string msg = ex.Message;

            Exception cur = ex;
            while (cur != null)
            {
                string f = FriendlyHResult(cur.HResult);
                if (f != null && friendly == null)
                {
                    friendly = f;
                    friendlyHr = cur.HResult;
                }
                if (cur != ex)
                    msg += " -> " + cur.Message;
                cur = cur.InnerException;
            }

            if (friendly != null)
                return friendly + " (0x" + ((uint)friendlyHr).ToString("X8") + ")";
            return msg;
        }

        private static string FriendlyHResult(int hr)
        {
            switch ((uint)hr)
            {
                case 0x80072F8F: // WININET_E_DECODING_FAILED / secure channel
                    return "Could not reach the Microsoft Update service securely. " +
                           "Check your internet connection, system date/time, and that the " +
                           "Windows Update service is enabled.";
                case 0x80072EE2: // timeout
                    return "The connection to Microsoft Update timed out. Check your internet connection.";
                case 0x8024402C: // proxy / connection
                    return "Could not connect to Microsoft Update. A proxy or firewall may be blocking it.";
                case 0x80240438:
                    return "The Windows Update client could not reach the service endpoint.";
                case 0x80070422: // service disabled
                    return "The Windows Update service is disabled. Enable it to search for drivers.";
                default:
                    return null;
            }
        }
    }
}
