using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhonieCore.OS
{
    public class MediaFilesAdapter(PlayerState state)
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
            BashAdapter.Exec("chmod 777 " + dir.FullName);

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

            Task.Run(() => BashAdapter.Exec($"rm {current} && ln -s {directory} {current}"));
        }
    }
}
