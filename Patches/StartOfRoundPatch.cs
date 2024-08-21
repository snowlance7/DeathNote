using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static DeathNote.DeathNoteBase;

namespace DeathNote.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("EndOfGame")]
        private static void EndOfGamePrefix()
        {
            logger.LogDebug("In EndOfGamePrefix");

            if (configShinigamiEyes.Value && !configPermanentEyes.Value)
            {
                logger.LogDebug("Passed config checks");

                DeathController.ShinigamiEyesActivated = false;
                logger.LogDebug("Set ShinigamiEyesActivated to false");
                DeathController.EnemyNames = new List<string>();
                DeathController.ScannedEnemies = new List<string>();
                logger.LogDebug("Cleared EnemyNames");
                // bruh
                if (UIControllerScript.Instance == null) { return; }
                UIControllerScript.Instance.btnActivateEyes.style.display = DisplayStyle.Flex;
                UIControllerScript.Instance.lblSEDescription.text = "You may, in exchange of half of your life, acquire the power of the Shinigami Eyes, which will enable you to see an entity's name when looking at them.\nThis will reset at the end of the round.";
                UIControllerScript.Instance.lblSEDescription.style.color = UnityEngine.Color.black;
                logger.LogDebug("Showed ShinigamiEyesActivated");

                logger.LogDebug("Finished");
            }
        }
    }
}
