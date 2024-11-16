﻿using System;
using System.IO;
using System.Linq;

namespace PhonieCore
{
    public class Library
    {
        private const string MediaDirectory = "/media/";
        private const string Marker = "---O";

        public string GetFolderForId(string id)
        {
            return ResolveMarkedDirectory(id);
        }

        private string ResolveMarkedDirectory(string id)
        {
            RemoveMarker();

            var directory = SetMarkerToDirectory(id);

            return directory;
        }

        private static string SetMarkerToDirectory(string id)
        {
            var targetDirectory = Path.Combine(MediaDirectory, id, Marker);

            if (Directory.Exists(Path.Combine(MediaDirectory, id)))
            {
                Console.WriteLine($"{id} Mark");
                Directory.Move(MediaDirectory + id, targetDirectory);
            }
            else
            {
                Console.WriteLine($"{id} Create");
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
