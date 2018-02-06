using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;using System.Threading;
using System.Collections.Concurrent;

namespace RapidAutoPrint
{
    class Program
    {
        public static ConcurrentQueue<string> jobQ = new ConcurrentQueue<string>();

        static void Main(string[] args)
        {
            var config = loadConfig();

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = config["Path"];
            watcher.Filter = config["Filter"];
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;

            var existingFiles = Directory.EnumerateFiles(config["Path"], config["Filter"]);
            foreach (string existingFile in existingFiles)
            {
                QPrint(existingFile);
            }

            Thread worker = new Thread(printThread);
            worker.Start();

            Console.WriteLine("Press \'q\' to quit. Press \'c\' to configure.");
            while (true)
            {
                var ch = Console.ReadKey(false).Key;
                switch (ch)
                {
                    case ConsoleKey.Q:
                        Environment.Exit(0);
                        break;
                    case ConsoleKey.C:
                        loadConfig(true);
                        Console.WriteLine("Quitting to reload configuration.");
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            QPrint(e.FullPath);
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            QPrint(e.FullPath);
        }

        private static void QPrint(string path)
        {
            Console.WriteLine($"Enqueieing {path}");
            jobQ.Enqueue(path);
        }

        private static void printThread()
        {
            while (true)
            {
                string job;
                while (jobQ.TryDequeue(out job))
                {
                    DoPrint(job);
                }
                Thread.Sleep(100);
            }
        }

        private static void DoPrint(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("DoPrint called with empty string!", nameof(path));
            }

            if (File.Exists(path))
            {
                Console.WriteLine($"Printing {path}");
                ProcessStartInfo info = new ProcessStartInfo(path.Trim());
                info.Verb = "Print";
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                var p = Process.Start(info);
                p.WaitForExit();
                File.Delete(path);
            }
        }

        private static Dictionary<String,String> loadConfig(bool resetConfig=false)
        {
            var dict = new Dictionary<String, String>();
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = configFile.AppSettings.Settings;
            string path = null;
            string filter = null;

            if (resetConfig)
            {
                appSettings.Clear();
            }
            else
            {
                path = ConfigurationManager.AppSettings["Path"];
                filter = ConfigurationManager.AppSettings["Filter"];
            }
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("What path should I monitor?");
                path = Console.ReadLine();
                if (Directory.Exists(path))
                {
                    appSettings.Add("Path", path);
                }
                else
                {
                    Console.WriteLine("Invalid path!");
                    Environment.Exit(1);
                }
            }

            if (string.IsNullOrEmpty(filter))
            {
                Console.WriteLine("What filename should I monitor (accepts wildcards)?");
                filter = Console.ReadLine();
                appSettings.Add("Filter", filter);
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            dict.Add("Path", path);
            dict.Add("Filter", filter);
            return dict;
        }
    }
}
