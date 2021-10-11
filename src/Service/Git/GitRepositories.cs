using ChaKi.Common.Settings;
using ChaKi.GUICommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Git
{
    public class GitRepositories
    {
        private static FileSystemWatcher Watcher;

        public static readonly string BasePath;

        public static event EventHandler<RepositoryChangedEventArgs> RepositoryChanged;

        public static string GetRepositoryPath(string name)
        {
            return Path.Combine(BasePath, name);
        }

        static GitRepositories()
        {
            BasePath = GitSettings.Current.LocalRepositoryBasePath;
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }

            Watcher = new FileSystemWatcher();
            Watcher.Path = BasePath;
            Watcher.Filter = "*.ann";
            Watcher.IncludeSubdirectories = true;
            Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            Watcher.Changed += Watcher_Changed;
            Watcher.EnableRaisingEvents = true;
        }

        public static void Enable()
        {
            Watcher.EnableRaisingEvents = true;
        }

        public static void Disable()
        {
            Watcher.EnableRaisingEvents = false;
        }


        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!Watcher.EnableRaisingEvents) return;

            Console.WriteLine($"{e.Name}");
            var name = Path.GetFileNameWithoutExtension(e.Name);
            RepositoryChanged?.Invoke(null, new RepositoryChangedEventArgs(name, e.FullPath));
        }

    }
}
