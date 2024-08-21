using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static DeathNote.DeathNoteBase;
//using TMPro;

namespace DeathNote
{
    public class UIControllerScript : MonoBehaviour
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        public static UIControllerScript Instance;
        private DeathController deathController;

        public VisualElement veMain;
        public ScrollView svRight;
        public int timeRemaining = configTimerLength.Value;
        private bool verifying = false;
        private bool showingUI = false;


        public Label lblResult;
        public TextField txtName;
        public DropdownField dpdnPlayerList;
        public DropdownField dpdnScannedEnemiesList;
        public Button btnSubmit;
        public TextField txtTimeOfDeath;
        public DropdownField dpdnDeathType;
        public DropdownField dpdnDetails;
        public ProgressBar pbRemainingTime;
        public Label lblSETitle;
        public Label lblSEDescription;
        public Button btnActivateEyes;

        private void Start()
        {
            logger.LogDebug("UIControllerScript: Start()");

            if (Instance == null)
            {
                Instance = this;
            }

            timeRemaining = configTimerLength.Value;

            // Get UIDocument
            logger.LogDebug("Getting UIDocument");
            UIDocument uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) { logger.LogError("uiDocument not found."); return; }

            // Get VisualTreeAsset
            logger.LogDebug("Getting visual tree asset");
            if (uiDocument.visualTreeAsset == null) { logger.LogError("visualTreeAsset not found."); return; }
            
            // Instantiate root
            VisualElement root = uiDocument.visualTreeAsset.Instantiate();
            if (root == null) { logger.LogError("root is null!"); return; }
            logger.LogDebug("Adding root");
            uiDocument.rootVisualElement.Add(root);
            if (uiDocument.rootVisualElement == null) { logger.LogError("uiDocument.rootVisualElement not found."); return; }
            logger.LogDebug("Got root");
            root = uiDocument.rootVisualElement;

            veMain = uiDocument.rootVisualElement.Q<VisualElement>("veMain");
            veMain.style.display = DisplayStyle.None;
            if (veMain == null) { logger.LogError("veMain not found."); return; }

            svRight = uiDocument.rootVisualElement.Q<ScrollView>("svRight");
            if (svRight == null) { logger.LogError("svRight not found."); return; }

            // Find elements
            lblResult = root.Q<Label>("lblResult");
            if (lblResult == null) { logger.LogError("lblResult not found."); return; }

            txtName = root.Q<TextField>("txtName");
            if (txtName == null) { logger.LogError("txtName not found."); return; }

            if (configShowPlayerList.Value)
            {
                dpdnPlayerList = root.Q<DropdownField>("dpdnPlayerList");
                if (dpdnPlayerList == null) { logger.LogError("dpdnPlayerList not found."); return; }
                dpdnPlayerList.choices.Add(" ");
                dpdnPlayerList.choices.AddRange(StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).Select(x => x.playerUsername).ToList());
                dpdnPlayerList.style.display = DisplayStyle.Flex;
            }

            if (configShowScannedEnemies.Value)
            {
                dpdnScannedEnemiesList = root.Q<DropdownField>("dpdnScannedEnemiesList");
                if (dpdnScannedEnemiesList == null) { logger.LogError("dpdnScannedEnemiesList not found."); return; }
                dpdnScannedEnemiesList.style.display = DisplayStyle.Flex;
            }

            btnSubmit = root.Q<Button>("btnSubmit");
            if (btnSubmit == null) { logger.LogError("btnSubmit not found."); return; }

            txtTimeOfDeath = root.Q<TextField>("txtTimeOfDeath");
            if (txtTimeOfDeath == null) { logger.LogError("txtTimeOfDeath not found."); return; }

            dpdnDeathType = root.Q<DropdownField>("dpdnDeathType");
            if (dpdnDeathType == null) { logger.LogError("dpdnDeathType not found."); return; }
            dpdnDeathType.choices = DeathController.GetCauseOfDeathsAsStrings();
            dpdnDeathType.index = 0;

            dpdnDetails = root.Q<DropdownField>("dpdnDetails");
            if (dpdnDetails == null) { logger.LogError("dpdnDetails not found."); return; }
            dpdnDetails.choices = DeathController.Details;
            dpdnDetails.index = 0;

            lblSETitle = root.Q<Label>("lblSETitle");
            if (lblSETitle == null) { logger.LogError("lblSETitle not found."); return; }

            lblSEDescription = root.Q<Label>("lblSEDescription");
            if (lblSEDescription == null) { logger.LogError("lblSEDescription not found."); return; }
            if (configPermanentEyes.Value) { lblSEDescription.text += "\nWARNING: This is permanent!"; } else { lblSEDescription.text += "\nThis will reset at the end of the round."; }

            btnActivateEyes = root.Q<Button>("btnActivateEyes");
            if (btnActivateEyes == null) { logger.LogError("btnActivateEyes not found."); return; }

            txtTimeOfDeath = root.Q<TextField>("txtTimeOfDeath");
            if (txtTimeOfDeath == null) { logger.LogError("txtTimeOfDeath not found."); return; }

            VisualElement veLeft = root.Q<VisualElement>("veLeft");
            int index = veLeft.IndexOf(txtTimeOfDeath);
            pbRemainingTime = new ProgressBar();
            pbRemainingTime.name = "pbRemainingTime";
            pbRemainingTime.title = "Time Remaining";
            pbRemainingTime.style.flexGrow = 0.90f;
            pbRemainingTime.style.display = DisplayStyle.None;
            veLeft.Insert(index + 1, pbRemainingTime);

            pbRemainingTime = root.Q<ProgressBar>("pbRemainingTime");
            if (pbRemainingTime == null) { logger.LogError("pbRemainingTime not found."); return; }
            pbRemainingTime.highValue = timeRemaining;

            lblSEDescription = root.Q<Label>("lblSEDescription");
            if (lblSEDescription == null) { logger.LogError("lblSEDescription not found."); return; }

            btnActivateEyes = root.Q<Button>("btnActivateEyes");
            if (btnActivateEyes == null) { logger.LogError("btnActivateEyes not found."); return; }
            
            if (!configShinigamiEyes.Value)
            {
                btnActivateEyes.style.display = DisplayStyle.None;
                lblSETitle.style.display = DisplayStyle.None;
                lblSEDescription.style.display = DisplayStyle.None;
            }
            
            logger.LogDebug("Got Controls for UI");

            // Add event handlers
            btnSubmit.clickable.clicked += BtnSubmitOnClick;
            btnActivateEyes.RegisterCallback<ClickEvent>(BtnActivateEyesOnClick);
            txtName.RegisterCallback<KeyUpEvent>(txtPlayerUsernameOnValueChanged);

            if (!configTimer.Value)
            {
                txtTimeOfDeath.style.display = DisplayStyle.Flex;
                txtTimeOfDeath.value = "";
                dpdnDeathType.style.display = DisplayStyle.Flex;
                dpdnDeathType.index = 0;
                dpdnDetails.style.display = DisplayStyle.Flex;
                dpdnDetails.index = 0;
            }

            logger.LogDebug("UIControllerScript: Start() complete");
        }

        private void Update()
        {
            if (veMain.style.display == DisplayStyle.Flex && Keyboard.current.escapeKey.wasPressedThisFrame) { HideUI(); }
            if (showingUI)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                StartOfRound.Instance.localPlayerUsingController = false;
                IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                StartOfRound.Instance.localPlayerController.disableLookInput = true;
            }
        }

        public void ShowUI()
        {
            logger.LogDebug("Showing UI");
            showingUI = true;
            veMain.style.display = DisplayStyle.Flex;

            if (DeathController.ShinigamiEyesActivated == true) { btnActivateEyes.style.display = DisplayStyle.None; }

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            StartOfRound.Instance.localPlayerUsingController = false;
            IngamePlayerSettings.Instance.playerInput.DeactivateInput();
            StartOfRound.Instance.localPlayerController.disableLookInput = true;

            if (configShowScannedEnemies.Value)
            {
                dpdnScannedEnemiesList.choices.Clear();
                dpdnScannedEnemiesList.choices.Add("");
                dpdnScannedEnemiesList.choices.AddRange(DeathController.ScannedEnemies);
            }
        }

        public void HideUI()
        {
            logger.LogDebug("Hiding UI");
            showingUI = false;
            veMain.style.display = DisplayStyle.None;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            StartOfRound.Instance.localPlayerUsingController = false;
            IngamePlayerSettings.Instance.playerInput.ActivateInput();
            StartOfRound.Instance.localPlayerController.disableLookInput = false;
        }

        public void ResetUI()
        {
            txtName.value = "";
            txtName.isReadOnly = false;
            txtTimeOfDeath.value = "";
            dpdnDeathType.index = 0;
            dpdnDetails.index = 0;
            pbRemainingTime.style.display = DisplayStyle.None;
            pbRemainingTime.highValue = 0;
            pbRemainingTime.lowValue = 0;
            pbRemainingTime.value = 0;

            if (configTimer.Value)
            {
                dpdnDeathType.style.display = DisplayStyle.None;
                dpdnDetails.style.display = DisplayStyle.None;
                txtTimeOfDeath.style.display = DisplayStyle.None;
            }

            if (configShowPlayerList.Value) { dpdnPlayerList.index = 0; }

            verifying = false;
        }

        private void StartProgressBarTimer(DeathController deathController)
        {
            StartCoroutine(StartProgressBarTimerCoroutine(deathController));
        }
        private IEnumerator StartProgressBarTimerCoroutine(DeathController deathController)
        {
            pbRemainingTime.lowValue = 0;
            pbRemainingTime.highValue = timeRemaining;

            txtTimeOfDeath.value = TimeToClock(TimeOfDay.Instance.currentDayTime + timeRemaining);
            float elapsedTime = 0f;

            while (pbRemainingTime.value < pbRemainingTime.highValue)
            {
                if (!verifying)
                {
                    break;
                }
                elapsedTime += Time.deltaTime * TimeOfDay.Instance.globalTimeSpeedMultiplier;
                pbRemainingTime.value = Mathf.Lerp(pbRemainingTime.lowValue, pbRemainingTime.highValue, elapsedTime / timeRemaining);
                yield return null;
            }

            if (!deathController.IsEntityDead())
            {
                deathController.causeOfDeathString = dpdnDeathType.value;
                deathController.detailsString = dpdnDetails.value;

                deathController.TimeOfDeathString = txtTimeOfDeath.text;
                float _timeOfDeath = ClockToTime(txtTimeOfDeath.text);
                if (_timeOfDeath == -1 || !(TimeOfDay.Instance.currentDayTime < _timeOfDeath) || !(_timeOfDeath < TimeOfDay.Instance.totalTime))
                {
                    if (deathController.PlayerToDie != null)
                    {
                        deathController.KillPlayer();
                    }
                    else
                    {
                        deathController.KillEnemy();
                    }
                }
                else
                {
                    deathController.TimeOfDeath = _timeOfDeath;
                    StartKillTimer(deathController);
                }
            }

            ResetUI();
        }

        public void StartKillTimer(DeathController deathController)
        {
            logger.LogDebug("Starting kill timer");
            StartCoroutine(deathController.StartKillTimerCoroutine());
        }

        private void BtnSubmitOnClick()
        {
            // normalizedTime = currentDayTime / totalTime;
            logger.LogDebug("BtnSubmitOnClick");

            if (StartOfRound.Instance.inShipPhase) { ShowResults("Ship must be landed to use this book", 5f, true); return; }

            if (verifying && configTimer.Value)
            {
                if (!configAllowEarlySubmit.Value) { return; }
                float time = ClockToTime(txtTimeOfDeath.text);
                if (time == -1)
                {
                    ShowResults("Wrong time format or out of reach. Format: 00:00AM/PM", 5, false);
                    return;
                }

                verifying = false;
                return;
            }


            deathController = new DeathController();
            PlayerControllerB playerToDie = StartOfRound.Instance.allPlayerScripts.ToList().Where(x => x.playerUsername.ToLower() == txtName.text.ToLower()).FirstOrDefault();
            string enemyName = DeathController.EnemyNames.Where(x => x.ToLower().Replace(" ", "") == txtName.text.ToLower().Replace(" ", "")).FirstOrDefault();
            if (enemyName == null && configShowScannedEnemies.Value && dpdnScannedEnemiesList.value != "")
            {
                enemyName = dpdnScannedEnemiesList.value;
            }
            
            if (enemyName != null)
            {
                int index = int.Parse(enemyName.Substring(enemyName.LastIndexOf('-') + 1));

                EnemyAI enemy = RoundManager.Instance.SpawnedEnemies.Where(x => x.thisEnemyIndex == index).FirstOrDefault();
                if (enemy == null) { logger.LogError($"Could not find enemy with index: {index}"); ShowResults("Error: Could not find entity to kill", 5, true); return; }

                if (enemy.isEnemyDead) { ShowResults("Enemy is already dead"); return; }
                logger.LogDebug($"Found enemy to kill: {enemyName}");
                ShowResults($"Found enemy to kill: {enemyName}", 5, true);

                deathController.EnemyName = enemyName;
                deathController.EnemyToDie = enemy;

                if (!configTimer.Value)
                {
                    deathController.TimeOfDeathString = txtTimeOfDeath.text;
                    float _timeOfDeath = ClockToTime(txtTimeOfDeath.text);
                    if (_timeOfDeath == -1 || !(TimeOfDay.Instance.currentDayTime < _timeOfDeath) || !(_timeOfDeath < TimeOfDay.Instance.totalTime))
                    {
                        deathController.KillEnemy();
                    }
                    else
                    {
                        deathController.TimeOfDeath = _timeOfDeath;
                        StartKillTimer(deathController);
                    }

                    ResetUI();
                    return;
                }

                txtName.isReadOnly = true;
                txtTimeOfDeath.style.display = DisplayStyle.Flex;
                txtTimeOfDeath.value = "";
                pbRemainingTime.style.display = DisplayStyle.Flex;

                verifying = true;
                StartProgressBarTimer(deathController);
            }
            else
            {
                if (playerToDie == null)
                {
                    if (!configShowPlayerList.Value || dpdnPlayerList.value == "")
                    {
                        ShowResults("Could not find entity!", 5f, true);
                        return;
                    }
                    else
                    {
                        playerToDie = StartOfRound.Instance.allPlayerScripts.Where(x => x.playerUsername == dpdnPlayerList.value).FirstOrDefault();
                        if (playerToDie == null) { logger.LogError("Couldnt find playerToDie in btnSubmitOnClick"); }
                    }
                }

                if (playerToDie.isPlayerDead) { ShowResults("Player is already dead"); return; }
                logger.LogDebug($"Found player to kill: {playerToDie.playerUsername}");
                ShowResults($"Found player to kill: {playerToDie.playerUsername}", 5, true);

                deathController.PlayerToDie = playerToDie;

                if (!configTimer.Value)
                {
                    deathController.causeOfDeathString = dpdnDeathType.value;
                    deathController.detailsString = dpdnDetails.value;

                    deathController.TimeOfDeathString = txtTimeOfDeath.text;
                    float _timeOfDeath = ClockToTime(txtTimeOfDeath.text);
                    if (_timeOfDeath == -1 || !(TimeOfDay.Instance.currentDayTime < _timeOfDeath) || !(_timeOfDeath < TimeOfDay.Instance.totalTime))
                    {
                        deathController.KillPlayer();
                    }
                    else
                    {
                        deathController.TimeOfDeath = _timeOfDeath;
                        StartKillTimer(deathController);
                    }

                    ResetUI();
                    return;
                }

                txtName.isReadOnly = true;
                txtTimeOfDeath.style.display = DisplayStyle.Flex;
                txtTimeOfDeath.value = "";
                dpdnDeathType.style.display = DisplayStyle.Flex;
                dpdnDeathType.index = 0;
                dpdnDetails.style.display = DisplayStyle.Flex;
                dpdnDetails.index = 0;
                pbRemainingTime.style.display = DisplayStyle.Flex;

                verifying = true;
                StartProgressBarTimer(deathController);
            }
        }

        private void txtPlayerUsernameOnValueChanged(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                BtnSubmitOnClick();
            }
        }

        public string TimeToClock(float time) // THIS WORKS
        {
            int numberOfHours = TimeOfDay.Instance.numberOfHours;
            float timeNormalized = time / TimeOfDay.Instance.totalTime;
            int num = (int)(timeNormalized * (60f * numberOfHours)) + 360;
            logger.LogDebug($"num: {num}");
            int num2 = (int)Mathf.Floor(num / 60);
            logger.LogDebug($"num2: {num2}");

            string amPM = "AM";
            if (num2 >= 24)
            {
                return "12:00AM";
            }
            if (num2 < 12)
            {
                amPM = "AM";
            }
            else
            {
                amPM = "PM";
            }
            if (num2 > 12)
            {
                num2 %= 12;
                logger.LogDebug($"num2 changed: {num2}");
            }
            int num3 = num % 60;
            logger.LogDebug($"num3: {num3}");
            string text = $"{num2:00}:{num3:00}".TrimStart('0') + amPM;
            return text;
        }

        public float ClockToTime(string timeString) // THIS WORKS DONT TOUCH FOR THE LOVE OF GOD
        {
            timeString = timeString.ToUpper().Replace(" ", "").Replace("\n", "");

            int numberOfHours = TimeOfDay.Instance.numberOfHours;
            float lengthOfHours = TimeOfDay.Instance.lengthOfHours;
            float totalTime = TimeOfDay.Instance.totalTime;

            int startHour = (24 - numberOfHours);

            // Split the time string into hours, minutes, and AM/PM
            string[] timeParts = timeString.Split(':');

            int hours;
            int minutes;

            if (!int.TryParse(timeParts[0], out hours)) { return -1; }
            if (!int.TryParse(timeParts[1].Substring(0, 2), out minutes)) { return -1; }

            string amPm = timeParts[1].Substring(2); // Extract AM/PM

            // Convert hours to 24-hour format
            if (amPm == "PM")
            {
                hours += 12;
            }
            else if (amPm == "AM" && hours == 12)
            {
                hours = 0;
            }


            // Calculate the total number of minutes
            hours = hours - startHour;
            float totalMinutes = (hours * lengthOfHours) + minutes;

            return totalMinutes;
        }

        public void ShowResults(string message, float duration = 3f, bool flash = false)
        {
            lblResult.text = message;
            StartCoroutine(ShowResultsCoroutine(message, duration, flash));
        }
        private IEnumerator ShowResultsCoroutine(string message, float duration, bool flash)
        {
            float endTime = Time.time + duration;
            bool isRed = true;

            if (flash == true)
            {
                while (Time.time < endTime)
                {
                    if (isRed)
                    {
                        lblResult.style.color = Color.black;
                    }
                    else
                    {
                        lblResult.style.color = Color.red;
                    }

                    isRed = !isRed;

                    yield return new WaitForSeconds(0.75f);
                }
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }


            lblResult.style.color = Color.red;
            lblResult.text = "";
        }

        private void BtnActivateEyesOnClick(ClickEvent evt)
        {
            logger.LogDebug("BtnActivateEyesOnClick");

            btnActivateEyes.style.display = DisplayStyle.None;
            lblSEDescription.text = "You have the shinigami eyes. You can now see entity names. This will reset after the round is over.";
            lblSEDescription.style.color = Color.red;

            //PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            //StartOfRound.Instance.localPlayerController.DamagePlayer(localPlayer.health / 2);

            DeathController.ShinigamiEyesActivated = true;
        }
    }
}
