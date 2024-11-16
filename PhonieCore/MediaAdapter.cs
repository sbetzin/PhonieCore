using System.IO;
using System.Linq;

namespace PhonieCore
{
    public class MediaAdapter
    {
        private const string MediaDirectory = "/media/";
        private const string Marker = "---O";

        public static string GetFolderForId(string id)
        {
            return ResolveMarkedDirectory(id);
        }

        private static string ResolveMarkedDirectory(string id)
        {
            RemoveMarker();

            var directory = SetMarkerToDirectory(id);

            return directory;
        }

        private static string SetMarkerToDirectory(string id)
        {
            var directory = Path.Combine(MediaDirectory, id);
            var targetDirectory = Path.Combine(MediaDirectory, $"{id}{Marker}");

            if (Directory.Exists(directory))
            {
                Directory.Move(directory, targetDirectory);
            }
            else
            {
                var dir = Directory.CreateDirectory(targetDirectory);
                Bash.Exec("sudo chmod 777 " + dir.FullName);
            }

            return targetDirectory;
        }

        private static void RemoveMarker()
        {
            var markedDirectories = Directory.EnumerateDirectories(MediaDirectory).Where(directory => directory.EndsWith(Marker)).ToList();
            foreach (var directory in markedDirectories)
            {
                Directory.Move(directory, directory.Replace(Marker, string.Empty).Trim());
            }
        }
    }
}
