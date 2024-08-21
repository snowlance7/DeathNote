using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;

namespace DeathNote.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ShowNameBillboard")]
        public static bool ShowNameBillboardPrefix()
        {
            if (!DeathNoteBase.configAlwaysShowPlayerNames.Value && !DeathController.ShinigamiEyesActivated) { return false; }
            return true;
        }
    }
}
