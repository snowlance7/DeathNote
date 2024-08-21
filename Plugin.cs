using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using LethalLib;
using GameNetcodeStuff;
using DeathNote;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace DeathNote
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class DeathNoteBase : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.DeathNote";
        private const string modName = "DeathNote";
        private const string modVersion = "1.0.0";

        public static AssetBundle? DNAssetBundle;
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }

        public static DeathNoteBase PluginInstance { get; private set; } = null!;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);

        public static ConfigEntry<string> configLevelRarities;
        public static ConfigEntry<string> configCustomLevelRarities;
        public static ConfigEntry<bool> configTimer;
        public static ConfigEntry<int> configTimerLength;
        public static ConfigEntry<bool> configAllowEarlySubmit;
        public static ConfigEntry<bool> configShowPlayerList;

        public static ConfigEntry<bool> configShinigamiEyes;
        public static ConfigEntry<bool> configPermanentEyes;

        public static ConfigEntry<bool> configAlwaysShowPlayerNames;
        public static ConfigEntry<bool> configShowEnemyNames;
        public static ConfigEntry<bool> configShowScannedEnemies;

        public static ConfigEntry<bool> configShowUnkillableEnemyNames;
        public static ConfigEntry<bool> configLockUI;

        //public static ConfigEntry<bool> configKillWithDeathType;
        //public static ConfigEntry<int> configTimeToKillWithDeathType;

        //public static ConfigEntry<string> configCustomNames;

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;
            LoggerInstance.LogDebug($"Plugin {modName} loaded successfully.");

            NetcodePatcher();

            configLevelRarities = Config.Bind("General", "Level Rarities", "ExperimentationLevel:30, AssuranceLevel:30, VowLevel:30, OffenseLevel:25, AdamanceLevel:25, MarchLevel:25, RendLevel:20, DineLevel:20, TitanLevel:10, ArtificeLevel:15, EmbrionLevel:50, All:20, Modded:30", "Rarities for each level. See default for formatting.");
            configCustomLevelRarities = Config.Bind("General", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");

            configTimer = Config.Bind("General", "Timer", true, "If picking the death details should have a time limit.\nWhen you enter a name and click submit, you'll have x in-game seconds to fill in a time and details before it adds the name to the book.");
            configTimerLength = Config.Bind("General", "Timer Length", 40, "Timer length. 40 is lore accurate.");
            configAllowEarlySubmit = Config.Bind("General", "Allow Early Submit", true, "Allows you to click submit again to add it to the book early. Turn this off if you want a cooldown mechanic.");
            
            configShowPlayerList = Config.Bind("Accessibility", "Show PlayerList", false, "Show a dropdown list of players under the name input to select from instead.");

            configShinigamiEyes = Config.Bind("Shinigami Eyes", "Shinigami Eyes", true, "Allows you to trade half of your max health for the ability to see certain entity names (configurable in Names section).\nEnemy names require you to scan them.");
            configPermanentEyes = Config.Bind("Shinigami Eyes", "Permanent Eyes", false, "Makes Shinigami Eyes permanent. Disabling this will reset the ability at the end of every round.");

            configAlwaysShowPlayerNames = Config.Bind("Names", "Always Show Player Names", false, "Always shows player names above their head. Disabling this will only show player names when you have the Shinigami Eyes.");
            configShowEnemyNames = Config.Bind("Names", "ShowEnemyNames", true, "Allows you to see enemy names when scanning them if you have the Shinigami Eyes.");
            configShowScannedEnemies = Config.Bind("Names", "ShowScannedEnemies", false, "Allows you to see scanned enemies as a list in the death note if you have the Shinigami Eyes.");
            
            configShowUnkillableEnemyNames = Config.Bind("Experimental", "Show Unkillable Enemy Names", false, "Allows you to see the names of enemies that are immortal. WARNING: Killing them can break things or cause bugs.");
            configLockUI = Config.Bind("Experimental", "Lock UI", false, "Locks the UI when showing it which should help with renabling input when pressing certain keybinds.");
            //configKillWithDeathType = Config.Bind("Experimental", "Kill With Death Type", false, "If this is true, killing a player with certain death types will attempt to kill them with whatever monster or hazard that kills them that way before the time limit.\nIf this fails, it will kill them the normal way.");
            //configTimeToKillWithDeathType = Config.Bind("Experimental", "Time To Kill With Death Type", 120, "Time in seconds to kill with death type before killing them normally.");

            // Loading assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            DNAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "dn_assets"));
            LoggerInstance.LogDebug($"Got DNAssetBundle at: {Path.Combine(sAssemblyLocation, "dn_assets")}");
            if (DNAssetBundle == null)
            {
                LoggerInstance.LogError("Failed to load custom assets.");
                return;
            }

            // Getting item
            LoggerInstance.LogDebug("Getting item");
            Item DeathNote = DNAssetBundle.LoadAsset<Item>("Assets/DeathNote/DeathNoteItem.asset");
            LoggerInstance.LogDebug($"Got item: {DeathNote.name}");

            string levelRarities = configLevelRarities.Value;
            string customLevelRarities = configCustomLevelRarities.Value;
            Dictionary<Levels.LevelTypes, int> levelRaritiesDict = new Dictionary<Levels.LevelTypes, int>();
            Dictionary<string, int> customLevelRaritiesDict = new Dictionary<string, int>();

            if (levelRarities != null)
            {
                string[] levels = levelRarities.Split(',');

                foreach (string level in levels)
                {
                    string[] levelSplit = level.Split(':');
                    if (levelSplit.Length != 2) { continue; }
                    string levelType = levelSplit[0].Trim();
                    string levelRarity = levelSplit[1].Trim();

                    if (Enum.TryParse<Levels.LevelTypes>(levelType, out Levels.LevelTypes levelTypeEnum) && int.TryParse(levelRarity, out int levelRarityInt))
                    {
                        levelRaritiesDict.Add(levelTypeEnum, levelRarityInt);
                    }
                    else
                    {
                        LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                    }
                }
            }
            if (customLevelRarities != null)
            {
                string[] levels = customLevelRarities.Split(',');

                foreach (string level in levels)
                {
                    string[] levelSplit = level.Split(':');
                    if (levelSplit.Length != 2) { continue; }
                    string levelType = levelSplit[0].Trim();
                    string levelRarity = levelSplit[1].Trim();

                    if (int.TryParse(levelRarity, out int levelRarityInt))
                    {
                        customLevelRaritiesDict.Add(levelType, levelRarityInt);
                    }
                    else
                    {
                        LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                    }
                }
            }

            // Register Scrap

            NetworkPrefabs.RegisterNetworkPrefab(DeathNote.spawnPrefab);
            Utilities.FixMixerGroups(DeathNote.spawnPrefab);
            Items.RegisterScrap(DeathNote, levelRaritiesDict, customLevelRaritiesDict);

            harmony.PatchAll();
            
            LoggerInstance.LogInfo($"{modGUID} v{modVersion} has loaded!");
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}