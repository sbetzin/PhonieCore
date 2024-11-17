using System.IO;
using System.Linq;

namespace PhonieCore
{
    public class MediaAdapter
    {
        private const string MediaDirectory = "/media/";

        public static string GetFolderForId(string id)
        {
            return ResolveMarkedDirectory(id);
        }

        private static string ResolveMarkedDirectory(string id)
        {
            return ResolveDirectory(id);
        }

        private static string ResolveDirectory(string id)
        {
            var directory = Path.Combine(MediaDirectory, id);

            if (Directory.Exists(directory))
            {
                return directory;
            }

            var dir = Directory.CreateDirectory(directory);
            BashAdapter.Exec("sudo chmod 777 " + dir.FullName);

            return directory;
        }
    }
}
