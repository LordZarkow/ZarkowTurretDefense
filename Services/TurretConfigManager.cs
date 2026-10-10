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

            return Parse(jsonResourceFile);
        }

        /// <summary>Parses a turret list from JSON text (built-in resource or a user's override file).</summary>
        public static List<TurretConfig> Parse(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<TurretConfig>>(json);
        }
    }
}
