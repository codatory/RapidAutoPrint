using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace RapidAutoPrint
{
    class Program
    {
        static void Main(string[] args)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = configFile.AppSettings.Settings;

            var path = ConfigurationManager.AppSettings["Path"];
            var filter = ConfigurationManager.AppSettings["Filter"];

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("What path should I monitor?");
                path = Console.ReadLine();
                appSettings.Add("Path", path);
            }
            if (string.IsNullOrEmpty(filter))
            {
                Console.WriteLine("What filename should I monitor (accepts wildcards)?");
                filter = Console.ReadLine();
                appSettings.Add("Filter", filter);
            }
            configFile.Save(ConfigurationSaveMode.Modified);

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
            watcher.Filter = filter;

            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;

            var existingFiles = Directory.EnumerateFiles(path, filter);
            foreach (string existingFile in existingFiles)
            {
                DoPrint(existingFile);
            }

            Console.WriteLine("Press \'q\' to quit.");
            while (Console.Read() != 'q') ;
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            DoPrint(e.FullPath);
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            DoPrint(e.FullPath);
        }

        private static void DoPrint(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("DoPrint called with empty string!", nameof(path));
            }

            if (File.Exists(path))
            {
                ProcessStartInfo info = new ProcessStartInfo(path.Trim());
                info.Verb = "Print";
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                var p = Process.Start(info);
                p.WaitForExit();
                File.Delete(path);
            }
        }
    }
}
