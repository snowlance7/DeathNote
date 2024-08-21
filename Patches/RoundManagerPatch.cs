using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static DeathNote.DeathNoteBase;

namespace DeathNote.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(RoundManager.FinishGeneratingLevel))]
        private static void FinishGeneratingLevelPostfix()
        {
            if (DeathController.ShinigamiEyesActivated)
            {
                localPlayer.DamagePlayer(50, false, true);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
            }
        }
    }
}
