using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using ZarkowTurretDefense.Models;

namespace ZarkowTurretDefense.Services
{
    using Scripts;

    /// <summary>
    /// Selects the turret / building-part list to use: a user file (named by a config setting, by
    /// default next to the mod's .cfg in BepInEx/config) replaces the built-in list wholesale; the
    /// built-in list is used only when no file exists or the file is unreadable. Also exports the
    /// built-in lists on request.
    /// </summary>
    static class ConfigFileService
    {
        public const string TurretsKind = "turrets";
        public const string BuildingpartsKind = "buildingparts";

        public const string TurretsResource = "ZarkowTurretDefense.Assets.Configs.turretsconfigs.json";
        public const string BuildingpartsResource = "ZarkowTurretDefense.Assets.Configs.buildingpartsconfigs.json";

        /// <summary>Default value of the list-file setting: &lt;GUID&gt;.&lt;kind&gt;.json in BepInEx/config.</summary>
        public static string DefaultOverrideFileName(string kind)
        {
            return ZTurretDefense.PluginGUID + "." + kind + ".json";
        }

        /// <summary>BepInEx/config/&lt;GUID&gt;.&lt;kind&gt;.default.json - the exported built-in list.</summary>
        public static string ExportPath(string kind)
        {
            return Path.Combine(Paths.ConfigPath, ZTurretDefense.PluginGUID + "." + kind + ".default.json");
        }

        /// <summary>
        /// Resolves the list-file setting to a path: empty means "no file"; a relative name is taken
        /// inside BepInEx/config; an absolute path is used as given.
        /// </summary>
        public static string ResolveOverridePath(string setting)
        {
            if (string.IsNullOrWhiteSpace(setting))
            {
                return null;
            }

            var trimmed = setting.Trim();
            return Path.IsPathRooted(trimmed) ? trimmed : Path.Combine(Paths.ConfigPath, trimmed);
        }

        public static string ReadResourceText(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Embedded resource not found", resourceName);
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>The user's list file as text, or an empty string when the setting is empty or the file does not exist.</summary>
        public static string ReadOverrideText(string kind, string setting)
        {
            var path = ResolveOverridePath(setting);
            if (path == null)
            {
                return string.Empty;
            }

            if (!File.Exists(path))
            {
                Jotunn.Logger.LogDebug($"### Config '{kind}': no list file at {path}");
                return string.Empty;
            }

            try
            {
                Jotunn.Logger.LogInfo($"### Config '{kind}': reading list file {path}");
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"### Config '{kind}': could not read {path}: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>Writes the built-in list to the export path. Failures are logged, never thrown.</summary>
        public static void ExportBuiltin(string kind, string resourceName)
        {
            var path = ExportPath(kind);
            try
            {
                File.WriteAllText(path, ReadResourceText(resourceName));
                Jotunn.Logger.LogInfo($"### Config '{kind}': built-in list exported to {path}");
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"### Config '{kind}': could not export built-in list to {path}: {e.Message}");
            }
        }

        /// <summary>
        /// Returns the list to use. <paramref name="overrideJson"/> empty: the built-in list. Otherwise the
        /// override is parsed, validated and used as the complete list; if it cannot be parsed the
        /// built-in list is used and an error is logged.
        /// </summary>
        public static List<T> SelectList<T>(string kind, string resourceName, string overrideJson,
            Func<string, List<T>> parse, Func<T, string> nameOf, Func<List<T>, List<string>> validate)
        {
            var embeddedText = ReadResourceText(resourceName);

            if (string.IsNullOrWhiteSpace(overrideJson))
            {
                Jotunn.Logger.LogInfo($"### Config '{kind}': built-in list (no list file)");
                return parse(embeddedText) ?? new List<T>();
            }

            try
            {
                var list = parse(overrideJson);
                if (list == null)
                {
                    throw new FormatException("root is not a JSON array");
                }

                var warnings = validate(list) ?? new List<string>();

                var missing = (parse(embeddedText) ?? new List<T>())
                    .Select(nameOf)
                    .Except(list.Select(nameOf))
                    .ToList();

                Jotunn.Logger.LogInfo($"### Config '{kind}': list file in use, {list.Count} entries, {warnings.Count} warning(s)");
                foreach (var warning in warnings)
                {
                    Jotunn.Logger.LogWarning($"### Config '{kind}': {warning}");
                }

                if (missing.Count > 0)
                {
                    Jotunn.Logger.LogInfo($"### Config '{kind}': built-in entries not in the list file, so not in the game: {string.Join(", ", missing)}");
                }

                return list;
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"### Config '{kind}': list file unreadable, built-in list in use: {e.Message}");
                return parse(embeddedText) ?? new List<T>();
            }
        }
    }

    /// <summary>Clamp-and-warn validation of user supplied lists. Never throws; returns the warnings.</summary>
    static class ConfigValidation
    {
        public static List<string> ValidateTurrets(List<TurretConfig> list)
        {
            var warnings = new List<string>();
            var seen = new HashSet<string>();

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var c = list[i];
                var label = string.IsNullOrEmpty(c.name) ? $"entry #{i}" : c.name;

                if (string.IsNullOrEmpty(c.name))
                {
                    warnings.Add($"{label}: missing name, entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (!seen.Add(c.name))
                {
                    warnings.Add($"{label}: duplicate name, later entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                TurretType parsedType;
                if (string.IsNullOrEmpty(c.type) || !Enum.TryParse(c.type, true, out parsedType))
                {
                    warnings.Add($"{label}: unknown type '{c.type}', entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(c.bundleName) || string.IsNullOrEmpty(c.prefabPath))
                {
                    warnings.Add($"{label}: bundleName and prefabPath are required, entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (c.range < 0f) { warnings.Add($"{label}: range {c.range} clamped to 0"); c.range = 0f; }
                if (c.minimumRange < 0f) { warnings.Add($"{label}: minimumRange {c.minimumRange} clamped to 0"); c.minimumRange = 0f; }
                if (c.fireInterval <= 0f) { warnings.Add($"{label}: fireInterval {c.fireInterval} set to 0.1"); c.fireInterval = 0.1f; }
                if (c.reloadTime < 0f) { warnings.Add($"{label}: reloadTime {c.reloadTime} clamped to 0"); c.reloadTime = 0f; }
                if (c.ammoCount < 0) { warnings.Add($"{label}: ammoCount {c.ammoCount} clamped to 0"); c.ammoCount = 0; }
                if (c.maximumNumberOfTrackedTargets < 1) { warnings.Add($"{label}: maximumNumberOfTrackedTargets {c.maximumNumberOfTrackedTargets} set to 1"); c.maximumNumberOfTrackedTargets = 1; }
                if (c.damageRadius < 0f) { warnings.Add($"{label}: damageRadius {c.damageRadius} clamped to 0"); c.damageRadius = 0f; }

                ClampDamage(label, "damage", ref c.damage, warnings);
                ClampDamage(label, "bluntDamage", ref c.bluntDamage, warnings);
                ClampDamage(label, "pierceDamage", ref c.pierceDamage, warnings);
                ClampDamage(label, "chopDamage", ref c.chopDamage, warnings);
                ClampDamage(label, "pickaxeDamage", ref c.pickaxeDamage, warnings);
                ClampDamage(label, "fireDamage", ref c.fireDamage, warnings);
                ClampDamage(label, "frostDamage", ref c.frostDamage, warnings);
                ClampDamage(label, "lightningDamage", ref c.lightningDamage, warnings);
                ClampDamage(label, "poisonDamage", ref c.poisonDamage, warnings);
                ClampDamage(label, "spiritDamage", ref c.spiritDamage, warnings);
                ClampDamage(label, "rangedDamage", ref c.rangedDamage, warnings);
                ClampDamage(label, "rangedBluntDamage", ref c.rangedBluntDamage, warnings);
                ClampDamage(label, "rangedPierceDamage", ref c.rangedPierceDamage, warnings);
                ClampDamage(label, "rangedChopDamage", ref c.rangedChopDamage, warnings);
                ClampDamage(label, "rangedPickaxeDamage", ref c.rangedPickaxeDamage, warnings);
                ClampDamage(label, "rangedFireDamage", ref c.rangedFireDamage, warnings);
                ClampDamage(label, "rangedFrostDamage", ref c.rangedFrostDamage, warnings);
                ClampDamage(label, "rangedLightningDamage", ref c.rangedLightningDamage, warnings);
                ClampDamage(label, "rangedPoisonDamage", ref c.rangedPoisonDamage, warnings);
                ClampDamage(label, "rangedSpiritDamage", ref c.rangedSpiritDamage, warnings);

                if (c.resources == null)
                {
                    warnings.Add($"{label}: resources missing, piece will have no build cost");
                    c.resources = new List<TurretConfigRequirement>();
                }
                else
                {
                    for (var r = c.resources.Count - 1; r >= 0; r--)
                    {
                        var req = c.resources[r];
                        if (req == null || string.IsNullOrEmpty(req.item))
                        {
                            warnings.Add($"{label}: resource without item name removed");
                            c.resources.RemoveAt(r);
                        }
                        else if (req.amount < 1)
                        {
                            warnings.Add($"{label}: resource '{req.item}' amount {req.amount} set to 1");
                            req.amount = 1;
                        }
                    }
                }
            }

            return warnings;
        }

        public static List<string> ValidateBuildingparts(List<BuildingpartConfig> list)
        {
            var warnings = new List<string>();
            var seen = new HashSet<string>();

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var c = list[i];
                var label = string.IsNullOrEmpty(c.name) ? $"entry #{i}" : c.name;

                if (string.IsNullOrEmpty(c.name))
                {
                    warnings.Add($"{label}: missing name, entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (!seen.Add(c.name))
                {
                    warnings.Add($"{label}: duplicate name, later entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                BuildingpartType parsedType;
                if (string.IsNullOrEmpty(c.type) || !Enum.TryParse(c.type, true, out parsedType))
                {
                    warnings.Add($"{label}: unknown type '{c.type}', entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(c.bundleName) || string.IsNullOrEmpty(c.prefabPath))
                {
                    warnings.Add($"{label}: bundleName and prefabPath are required, entry skipped");
                    list.RemoveAt(i);
                    continue;
                }

                if (c.resources == null)
                {
                    warnings.Add($"{label}: resources missing, piece will have no build cost");
                    c.resources = new List<BuildingpartConfigRequirement>();
                }
                else
                {
                    for (var r = c.resources.Count - 1; r >= 0; r--)
                    {
                        var req = c.resources[r];
                        if (req == null || string.IsNullOrEmpty(req.item))
                        {
                            warnings.Add($"{label}: resource without item name removed");
                            c.resources.RemoveAt(r);
                        }
                        else if (req.amount < 1)
                        {
                            warnings.Add($"{label}: resource '{req.item}' amount {req.amount} set to 1");
                            req.amount = 1;
                        }
                    }
                }
            }

            return warnings;
        }

        private static void ClampDamage(string label, string field, ref float value, List<string> warnings)
        {
            if (value < 0f)
            {
                warnings.Add($"{label}: {field} {value} clamped to 0");
                value = 0f;
            }
        }
    }
}
