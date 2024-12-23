﻿using System;
using System.IO;
using System.Linq;

namespace PhonieCore
{
    public class MediaAdapter(PlayerState state)
    {
        private string _currentDirectory;

        public string[] GetFilesForId(string id)
        {
            var directory = GetDirectoryForId(id);
            CreateSymlinkForId(directory);

            return Directory.EnumerateFiles(directory).ToArray();
        }

        private string GetDirectoryForId(string id)
        {
            var directory = Path.Combine(state.MediaFolder, id);

            if (Directory.Exists(directory)) return directory;

            var dir = Directory.CreateDirectory(directory);
            BashAdapter.Exec("sudo chmod 777 " + dir.FullName);

            return directory;
        }

        public void CreateSymlinkForId(string directory)
        {
            if (_currentDirectory == directory)
            {
                return;
            }

            _currentDirectory = directory;
            var current = Path.Combine(state.MediaFolder, "_current");

            BashAdapter.Exec(($"sudo rm {current} && sudo ln -s {directory} {current}"));
        }
    }
}
