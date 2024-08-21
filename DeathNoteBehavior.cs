using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeathNote
{
    internal class DeathNoteBehavior : PhysicsProp
    {
        private static ManualLogSource logger = DeathNoteBase.LoggerInstance;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Using item works!");
                
                UIControllerScript uiController = GetComponent<UIControllerScript>();
                logger.LogDebug("Got veMain");
                logger.LogMessage(uiController.veMain.style.display.ToString());
                if (uiController.veMain.style.display == null)
                {
                    logger.LogDebug("veMain.style.display is null");
                    return;
                }
                
                if (uiController.veMain.style.display == DisplayStyle.None)
                {
                    if (DeathController.ShinigamiEyesActivated)
                    {
                        uiController.btnActivateEyes.style.display = DisplayStyle.None;
                        uiController.lblSEDescription.text = "You have the shinigami eyes. You can now see entity names. This will reset after the round is over.";
                        uiController.lblSEDescription.style.color = Color.red;
                    }

                    logger.LogDebug("Showing UI");
                    uiController.ShowUI();
                }
            }
        }
    }
}