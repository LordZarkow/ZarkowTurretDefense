using System.Collections.Generic;
using ZarkowTurretDefense.Models;

namespace ZarkowTurretDefense.Services
{
    using System.IO;
    using System.Reflection;

    class TurretConfigManager
    {
        public static List<TurretConfig> LoadTurretConfigJsonFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string jsonResourceFile;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonResourceFile = reader.ReadToEnd(); //Make string equal to full file
            }

            return SimpleJson.SimpleJson.DeserializeObject<List<TurretConfig>>(jsonResourceFile);
        }
    }
}
