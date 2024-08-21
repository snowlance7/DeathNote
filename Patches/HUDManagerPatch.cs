using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeathNote.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            if (DeathController.ShinigamiEyesActivated && DeathNoteBase.configShowEnemyNames.Value)
            {
                logger.LogDebug($"Got {HUDManager.Instance.nodesOnScreen.Count} nodes");

                foreach (var node in HUDManager.Instance.nodesOnScreen)
                {
                    if(node.nodeType == 1)
                    {
                        logger.LogDebug($"{node.nodeType} {node.headerText} {node.subText}");

                        EnemyAI enemy = node.gameObject.GetComponentInParent<EnemyAI>();

                        if (!DeathNoteBase.configShowUnkillableEnemyNames.Value && !enemy.enemyType.canDie) { continue; }

                        logger.LogDebug($"Got enemy from scannode:{enemy.thisEnemyIndex}: {enemy.enemyType.enemyName}");
                        string name = DeathController.EnemyNames.Where(x => int.Parse(x.Substring(x.LastIndexOf('-') + 1)) == enemy.thisEnemyIndex).FirstOrDefault();
                        
                        if (name != null)
                        {
                            logger.LogDebug($"Found name: {name}");
                            node.headerText = name;
                            node.subText = enemy.enemyType.enemyName;
                            DeathController.ScannedEnemies.Add(name);
                        }
                    }
                }
            }
        }
    }
}
