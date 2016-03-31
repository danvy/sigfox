using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Danvy.Azure
{
    public static class WebJobsHelper
    {
        private static FileSystemWatcher watcher = null;
        static WebJobsHelper()
        {
            NeedShutdown = false;
            RunAsWebJobs = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBJOBS_RUN_ID"));
            ShutdownFile = Environment.GetEnvironmentVariable("%WEBJOBS_SHUTDOWN_FILE%");
            if (!string.IsNullOrEmpty(ShutdownFile))
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(ShutdownFile));
                watcher.Created += OnChanged;
                watcher.Changed += OnChanged;
                watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite;
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;
            }
        }
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.IndexOf(Path.GetFileName(ShutdownFile), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                NeedShutdown = true;
            }
        }
        public static bool RunAsWebJobs { get; private set; }
        private static string ShutdownFile { get; set; }
        public static bool NeedShutdown { get; private set; }
    }
}
