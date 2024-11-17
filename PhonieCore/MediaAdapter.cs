using System;
using System.IO;
using System.Linq;

namespace PhonieCore
{
    public class MediaAdapter(PlayerState state) : IDisposable
    {
        private readonly PlayerState _state = state;
        private FileSystemWatcher _watcher;

        public string[] GetFilesForId(string id)
        {
            var directory = GetDirectoryForId(id);
            CreateSymlinkForId(directory);

            return Directory.EnumerateFiles(directory).ToArray();
        }

        private string GetDirectoryForId(string id)
        {
            var directory = Path.Combine(_state.MediaFolder, id);

            if (Directory.Exists(directory)) return directory;

            var dir = Directory.CreateDirectory(directory);
            BashAdapter.Exec("sudo chmod 777 " + dir.FullName);

            return directory;
        }

        public void CreateSymlinkForId(string directory)
        {
            var current = Path.Combine(_state.MediaFolder, "_current");

            BashAdapter.Exec(($"sudo rm {current}"));
            BashAdapter.Exec(($"sudo ln -s {directory} {current}"));
        }

        public void WaitForChanges()
        {
            _watcher = new FileSystemWatcher(_state.MediaFolder);
            _watcher.Filters.Add("*.mp3");
            _watcher.Filters.Add("*.txt");
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.IncludeSubdirectories = true;
            _watcher.Created += Watcher_Changed;
            _watcher.Deleted += Watcher_Changed;
            _watcher.Renamed += Watcher_Changed;
            _watcher.EnableRaisingEvents = true;
        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Logging.Logger.Log(e.FullPath);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
