using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;

namespace RapidAutoPrint
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "c:\\printme";
            var filter = "*.pdf";

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
                throw new ArgumentException("message", nameof(path));
            }

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
