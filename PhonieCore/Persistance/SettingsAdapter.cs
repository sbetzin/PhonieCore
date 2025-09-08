using PhonieCore.Persistance.Model;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhonieCore.Persistance
{
    public static  class SettingsAdapter
    {
        public static async Task<Settings> LoadAsync()
        {
            using var stream = File.OpenRead("settings.json");
            return await JsonSerializer.DeserializeAsync<Settings>(stream);
        }

        public static async Task SaveAsync(Settings settings)
        {
            using var stream = File.Create("settings.json");
            await JsonSerializer.SerializeAsync(stream, settings);
        }
    }
}
