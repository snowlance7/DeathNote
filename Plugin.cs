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

namespace DeathNote
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class DeathNoteBase : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.DeathNote";
        private const string modName = "DeathNote";
        private const string modVersion = "0.4.1";

        public static AssetBundle? DNAssetBundle;

        public static DeathNoteBase PluginInstance { get; private set; } = null!;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);

        public static ConfigEntry<int> configRarity;
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

            configRarity = Config.Bind("General", "Rarity", 5, "Rarity of the death note.");
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

            DNAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "mod_assets"));
            LoggerInstance.LogDebug($"Got DNAssetBundle at: {Path.Combine(sAssemblyLocation, "mod_assets")}");
            if (DNAssetBundle == null)
            {
                LoggerInstance.LogError("Failed to load custom assets.");
                return;
            }

            // Getting item
            LoggerInstance.LogDebug("Getting item");
            Item DeathNote = DNAssetBundle.LoadAsset<Item>("Assets/DeathNote/DeathNoteItem.asset");
            LoggerInstance.LogDebug($"Got item: {DeathNote.name}");
            
            // Assign behavior script
            DeathNoteBehavior script = DeathNote.spawnPrefab.AddComponent<DeathNoteBehavior>();

            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = DeathNote;

            // Assign UIControllerScript
            LoggerInstance.LogDebug("Setting up UI");
            UIControllerScript uiController = DeathNote.spawnPrefab.AddComponent<UIControllerScript>(); // WHY TF IS THIS BREAKING NOW
            if (uiController == null) { LoggerInstance.LogError("uiController not found."); return; }
            LoggerInstance.LogDebug("Got UIControllerScript");

            // Register Scrap
            int iRarity = configRarity.Value;
            NetworkPrefabs.RegisterNetworkPrefab(DeathNote.spawnPrefab);
            Utilities.FixMixerGroups(DeathNote.spawnPrefab);
            Items.RegisterScrap(DeathNote, iRarity, Levels.LevelTypes.All);

            harmony.PatchAll();
            
            LoggerInstance.LogInfo($"{modGUID} v{modVersion} has loaded!");
        }

        private void Update()
        {
            if (StartOfRound.Instance != null && DeathController.ShinigamiEyesActivated)
            {
                PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
                if (localPlayer.health > (DeathController.HalfHealth))
                {
                    localPlayer.DamagePlayer(localPlayer.health - DeathController.HalfHealth, false, true, CauseOfDeath.Unknown, 0); // TODO: Test this more
                }
            }
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