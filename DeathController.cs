using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
//using TMPro;

namespace DeathNote
{
    public class DeathController : MonoBehaviour
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        public static bool ShinigamiEyesActivated = false;

        public static List<string> EnemyNames = new List<string>();
        public string? EnemyName;
        public EnemyAI EnemyToDie;

        public PlayerControllerB PlayerToDie = null;
        public static int MaxHealth = 100;
        public static int HalfHealth = MaxHealth / 2;
        public string causeOfDeathString;
        public string detailsString;

        public string TimeOfDeathString;
        public float TimeOfDeath;

        public static List<string> Details = new List<string> { "Heart Attack", "Decapitation", "Coil Decapitation", "Seizure", "Disappearance", "Mask", "Burn" }; // disapearance is 4


        public static List<string> GetCauseOfDeathsAsStrings()
        {
            List<string> deathType = new List<string>();

            deathType.Add(CauseOfDeath.Unknown.ToString());
            deathType.Add(CauseOfDeath.Abandoned.ToString());
            deathType.Add(CauseOfDeath.Blast.ToString());
            deathType.Add(CauseOfDeath.Bludgeoning.ToString());
            deathType.Add(CauseOfDeath.Burning.ToString());
            deathType.Add(CauseOfDeath.Crushing.ToString());
            deathType.Add(CauseOfDeath.Drowning.ToString());
            deathType.Add(CauseOfDeath.Electrocution.ToString());
            deathType.Add(CauseOfDeath.Fan.ToString());
            deathType.Add(CauseOfDeath.Gravity.ToString());
            deathType.Add(CauseOfDeath.Gunshots.ToString());
            deathType.Add(CauseOfDeath.Kicking.ToString());
            deathType.Add(CauseOfDeath.Mauling.ToString());
            deathType.Add(CauseOfDeath.Stabbing.ToString());
            deathType.Add(CauseOfDeath.Strangulation.ToString());
            deathType.Add(CauseOfDeath.Suffocation.ToString());

            return deathType;
        }

        // deathAnimations:
        // 0 = normal
        // 1 = decapitation
        // 2 = coilhead decapitation
        // 3 = seizure
        // 4 = disapearance // was masked
        // 5 = mask
        // 6 = burn

        public static CauseOfDeath GetCauseOfDeathFromString(string causeOfDeathString)
        {
            CauseOfDeath _causeOfDeath = CauseOfDeath.Unknown;
            
            switch (causeOfDeathString.ToLower())
            {
                case "abandoned":
                    _causeOfDeath = CauseOfDeath.Abandoned;
                    break;
                case "blast":
                    _causeOfDeath = CauseOfDeath.Blast;
                    break;
                case "bludgeoning":
                    _causeOfDeath = CauseOfDeath.Bludgeoning;
                    break;
                case "burning":
                    _causeOfDeath = CauseOfDeath.Burning;
                    break;
                case "crushing":
                    _causeOfDeath = CauseOfDeath.Crushing;
                    break;
                case "drowniing":
                    _causeOfDeath = CauseOfDeath.Drowning;
                    break;
                case "electrocution":
                    _causeOfDeath = CauseOfDeath.Electrocution;
                    break;
                case "fan":
                    _causeOfDeath = CauseOfDeath.Fan;
                    break;
                case "gravity":
                    _causeOfDeath = CauseOfDeath.Gravity;
                    break;
                case "gunshots":
                    _causeOfDeath = CauseOfDeath.Gunshots;
                    break;
                case "kicking":
                    _causeOfDeath = CauseOfDeath.Kicking;
                    break;
                case "mauling":
                    _causeOfDeath = CauseOfDeath.Mauling;
                    break;
                case "stabbing":
                    _causeOfDeath = CauseOfDeath.Stabbing;
                    break;
                case "strangulation":
                    _causeOfDeath = CauseOfDeath.Strangulation;
                    break;
                case "suffocation":
                    _causeOfDeath = CauseOfDeath.Suffocation;
                    break;
                case "unknown":
                    _causeOfDeath = CauseOfDeath.Unknown;
                    break;
            }

            logger.LogDebug($"Got cause of death: {_causeOfDeath}");
            return _causeOfDeath;
        }

        public void KillWithDeathType(CauseOfDeath causeOfDeath, int time) // TODO: Set up timer
        {
            logger.LogDebug("In KillWithDeathType");
        }

        public IEnumerator StartKillTimerCoroutine()
        {
            logger.LogDebug("In StartKillTimerCoroutine");

            Label lblEntityToDie = new Label();
            //lblEntityToDie.style.unityFont = DeathNoteBase.DNAssetBundle.LoadAsset<Font>("Assets/DeathNote/Death Note.ttf"); // TODO: figure out how to get this to work
            lblEntityToDie.style.color = Color.red;
            if (PlayerToDie != null) { lblEntityToDie.text = $"{PlayerToDie.playerUsername}: {causeOfDeathString}, {UIControllerScript.Instance.TimeToClock(TimeOfDeath)}"; }
            else { lblEntityToDie.text = $"{EnemyName}: {UIControllerScript.Instance.TimeToClock(TimeOfDeath)}"; }

            if (lblEntityToDie == null) { logger.LogError("lblEntityToDie is null"); }

            ProgressBar pbTimeToDie = new ProgressBar();
            pbTimeToDie.name = "pbTimeToDie";
            pbTimeToDie.lowValue = 0;
            pbTimeToDie.highValue = TimeOfDeath - TimeOfDay.Instance.currentDayTime;
            pbTimeToDie.style.display = DisplayStyle.Flex;
            pbTimeToDie.title = "Remaining Time";

            UIControllerScript.Instance.svRight.Add(lblEntityToDie);
            UIControllerScript.Instance.svRight.Add(pbTimeToDie);
            
            if (pbTimeToDie == null) { logger.LogError("pbTimeToDie is null"); }

            float elapsedTime = 0f;

            while (pbTimeToDie.value < pbTimeToDie.highValue)
            {
                if(IsEntityDead()) { break; }

                elapsedTime += Time.deltaTime * TimeOfDay.Instance.globalTimeSpeedMultiplier;
                pbTimeToDie.value = Mathf.Lerp(pbTimeToDie.lowValue, pbTimeToDie.highValue, elapsedTime / pbTimeToDie.highValue);
                yield return null;
            }

            UIControllerScript.Instance.svRight.Remove(lblEntityToDie);
            UIControllerScript.Instance.svRight.Remove(pbTimeToDie);

            if (PlayerToDie != null) { KillPlayer(); } else { KillEnemy(); }
        }

        public bool IsEntityDead()
        {
            if (PlayerToDie != null)
            {
                if (PlayerToDie.isPlayerDead) { return true; }
            }
            else
            {
                if (EnemyToDie.isEnemyDead) { return true; }
            }

            return false;
        }

        public void KillPlayer()
        {
            if (PlayerToDie.isPlayerDead) { UIControllerScript.Instance.ShowResults($"{PlayerToDie.playerUsername} has died already."); return; }
            logger.LogDebug($"Killing player {PlayerToDie.playerUsername}: {causeOfDeathString}, {TimeOfDeathString}");

            string[] info = { PlayerToDie.actualClientId.ToString(), causeOfDeathString, detailsString };
            NetworkHandler.clientMessage.SendServer(info);
        }

        public void KillEnemy()
        {
            if (IsEntityDead()) { UIControllerScript.Instance.ShowResults($"{EnemyName} has died already."); return; }

            try
            {
                EnemyToDie.enemyType.canDie = true;
                EnemyToDie.KillEnemyOnOwnerClient();
            }
            catch
            {
                logger.LogDebug("Error while attempting to kill the enemy");
            }
        }
    }
}
