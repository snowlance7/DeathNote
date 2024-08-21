using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeathNote.Patches
{
    [HarmonyPatch(typeof(QuickMenuManager))]
    internal class QuickMenuManagerPatch
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        [HarmonyPrefix]
        [HarmonyPatch("OpenQuickMenu")]
        public static bool OpenQuickMenuPatch()
        {
            if (UIControllerScript.Instance == null) { return true; }
            if (UIControllerScript.Instance.veMain == null) { logger.LogError("veMain is null!"); return true; }
            if (UIControllerScript.Instance.veMain.style.display == DisplayStyle.Flex) { return false; }
            return true;
        }
    }
}